using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Extensions;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MyCharMod.Mechanics;

/// <summary>
/// 阿尔法机所有卡牌的基类，把四个核心机制的通用流程收在一处：
///
/// 结算顺序固定为：
///   1. 响应（消费/读取传参池，逐条结算）
///   2. 本牌自身效果（α协议的增幅作用在这里）
///   3. 传参（重新入池）
///   4. 迭代（记一次打出）
///
/// 「响应」：打出时消费传参池。池空时也能照常打出，只是吃不到传参加成——避免响应牌卡在手里。
/// 子类只实现 <see cref="PlayEffect"/>（本牌效果）与 <see cref="BuildParam"/>（传参内容）。
/// </summary>
public abstract class AlphaCard : CustomCardModel
{
    private static readonly IReadOnlyDictionary<string, decimal> NoIterationVars =
        new Dictionary<string, decimal>();

    /// <summary>本牌本次结算的增幅（α协议）。1 = 无增幅，打完还原。</summary>
    private decimal _responseBoost = 1m;

    /// <summary>本牌本次结算的伤害专用增幅（凝神聚气）。只作用于 Damage，不动 Block。</summary>
    private decimal _damageBoost = 1m;

    /// <summary>本牌本次结算的**平伤**加成（传参 DamageAdd）。乘算增幅之后再叠加，打完还原。</summary>
    private decimal _damageAdd = 0m;

    /// <summary>是否正在结算本牌自身效果。用来区分「实战」（用本次吃到的增幅）与「预览」（用池里挂着的增幅）。</summary>
    private bool _resolving;

    /// <summary>本次打出时，响应是否真的消费到了传参（供「零基启动」判断是否回手）。每次打出刷新。</summary>
    protected bool ResponseConsumed { get; private set; }

    protected AlphaCard(int baseCost, CardType type, CardRarity rarity, TargetType target,
        bool showInCardLibrary = true)
        : base(baseCost, type, rarity, target, showInCardLibrary)
    {
    }

    /// <summary>是否带「响应」：打出时消费传参池。池空也能打出，只是没有加成。</summary>
    public virtual bool IsResponse => false;

    /// <summary>是否带「传参」：打出时向池里加入一条传参。</summary>
    public virtual bool IsParamSource => false;

    /// <summary>响应是否消耗传参池。继承迭代为 false（每次都能吃到池里的加成，但池留给别人）。</summary>
    public virtual bool ConsumesParams => true;

    /// <summary>完全响应：本次战斗吃到的传参永久挂在这张牌上，之后每次打出都重放一次。</summary>
    public virtual bool RetainsResponse => false;

    /// <summary>迭代间隔 X：这张牌每打出 X 次，就把迭代效果在本牌上叠加一次。0 = 不迭代。</summary>
    public virtual int IterationEvery => 0;

    /// <summary>
    /// 迭代效果加在哪些动态变量上（变量名 → 每叠加一次加多少），用于卡面实时显示数值。
    /// 只影响显示（见 <see cref="IterationPreviewPatch"/>）；实际结算由 <see cref="PlayEffect"/> 自己算。
    /// </summary>
    public virtual IReadOnlyDictionary<string, decimal> IterationVarBonus => NoIterationVars;

    /// <summary>是否在卡面挂上「协议」词条（会往协议层数里写牌的牌）。</summary>
    public virtual bool GrantsProtocol => false;

    /// <summary>本牌固有的原版关键词（消耗 / 虚无 / 固有…）。</summary>
    protected virtual IEnumerable<CardKeyword> BaseKeywords => Array.Empty<CardKeyword>();

    /// <summary>本场战斗中这张牌已经打出的次数。</summary>
    public int PlayCount => AlphaCorePower.Of(this)?.GetPlayCount(this) ?? 0;

    /// <summary>本场战斗已经打出的牌总数（供「零基启动」的迭代加成读取）。</summary>
    protected int TotalCardsPlayedThisCombat => AlphaCorePower.Of(this)?.TotalPlays ?? 0;

    /// <summary>迭代已叠加的层数 = 已打出次数 ÷ 迭代间隔。</summary>
    public int IterationStacks => AlphaCorePower.Of(this)?.GetIterationStacks(this, IterationEvery) ?? 0;

    /// <summary>某个动态变量当前累积的迭代加成 = 每次增量 × 已叠加层数。结算时传给 <see cref="ResolveValue"/>。</summary>
    protected virtual decimal IterationBonusFor(string varName) =>
        IterationVarBonus.TryGetValue(varName, out decimal perStack) ? perStack * IterationStacks : 0m;

    /// <summary>
    /// 本牌完成一次迭代（层数 +1）时的额外结算，在计数与事件广播之前调用。
    /// 用于「一次性迭代效果」（如逐步加载的给能量）或「按完成时机快照」的动态成长（如零基启动）。
    /// </summary>
    protected virtual Task OnIterationCompleted(PlayerChoiceContext choiceContext) => Task.CompletedTask;

    /// <summary>
    /// 卡面关键词 = 原版关键词 + 机制关键词。
    /// 挂上关键词后，卡面的词条标签和悬浮说明都由引擎自动生成，不用手写 ExtraHoverTips。
    /// </summary>
    public sealed override IEnumerable<CardKeyword> CanonicalKeywords
    {
        get
        {
            foreach (CardKeyword keyword in BaseKeywords)
            {
                yield return keyword;
            }
            if (IsParamSource)
            {
                yield return AlphaKeywords.ParamPassing;
            }
            if (IsResponse)
            {
                yield return AlphaKeywords.Response;
            }
            if (GrantsProtocol)
            {
                yield return AlphaKeywords.Protocol;
            }
            if (IterationEvery > 0)
            {
                yield return AlphaKeywords.Iteration;
            }
        }
    }

    /// <summary>卡面注入迭代计数器与「完全响应」已保留的传参：文案里用 {IterCount} / {Retained}。</summary>
    protected override void AddExtraArgsToDescription(LocString description)
    {
        base.AddExtraArgsToDescription(description);
        if (IterationEvery > 0)
        {
            description.Add("IterCount", (decimal)PlayCount);
            description.Add("IterEvery", (decimal)IterationEvery);
        }
        if (RetainsResponse)
        {
            description.Add("Retained", RetainedText());
        }
    }

    /// <summary>
    /// 「完全响应」已经挂在本牌上的传参。同类（种类 / 数值 / 子类型都相同）聚合计数，
    /// 例：`抽1张牌 ×3、回复5点生命 ×2`；没有则渲染成空串。
    /// </summary>
    private string RetainedText()
    {
        AlphaCorePower? core = AlphaCorePower.Of(this);
        if (core == null)
        {
            return "";
        }
        IReadOnlyList<ParamEntry> retained = core.GetRetained(this);
        if (retained.Count == 0)
        {
            return "";
        }
        IEnumerable<string> lines = retained
            .GroupBy(entry => (entry.Kind, entry.Value, entry.Variant))
            .Select(group => group.Count() == 1
                ? group.First().Describe()
                : $"{group.First().Describe()} ×{group.Count()}");
        return "\n已保留：" + string.Join("、", lines);
    }

    protected sealed override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        AlphaCorePower? core = AlphaCorePower.Of(this);
        decimal boost = 1m;
        decimal damageBoost = 1m;
        decimal damageAdd = 0m;

        // 1) 响应
        if (IsResponse && core != null)
        {
            var active = new List<ParamEntry>();
            if (RetainsResponse)
            {
                active.AddRange(core.GetRetained(this));
            }

            List<ParamEntry> entries = ConsumesParams ? core.ConsumeParams() : core.PeekParams();
            active.AddRange(entries);

            if (RetainsResponse && entries.Count > 0)
            {
                core.AddRetained(this, entries);
            }

            if (active.Count > 0)
            {
                // 先归拢增幅（EffectBoost 全属性 / DamageBoost 仅伤害 / DamageAdd 平伤），再结算其余传参。
                foreach (ParamEntry entry in active)
                {
                    if (entry.Kind == ParamKind.EffectBoost)
                    {
                        boost += entry.Value;
                    }
                    else if (entry.Kind == ParamKind.DamageBoost)
                    {
                        damageBoost += entry.Value;
                    }
                    else if (entry.Kind == ParamKind.DamageAdd)
                    {
                        damageAdd += entry.Value;
                    }
                }
                foreach (ParamEntry entry in active)
                {
                    if (entry.Kind is not (ParamKind.EffectBoost or ParamKind.DamageBoost
                        or ParamKind.DamageAdd))
                    {
                        await ApplyParam(choiceContext, cardPlay, entry);
                    }
                }

                core.BumpResponseOrIteration();
            }

            ResponseConsumed = active.Count > 0;

            // E2：只有真的吃到了传参（激活了池子的效果）才算「触发响应」→ 广播。
            // 池空时打出响应牌不算触发，避免「响应反馈」等监听者被空响应刷触发。
            if (ResponseConsumed)
            {
                await AlphaHooks.ResponseTriggered(choiceContext, this);
            }
        }

        // 2) 本牌自身效果（增幅由 RawDamage / RawBlock 计入，打完还原）
        _responseBoost = boost;
        _damageBoost = damageBoost;
        _damageAdd = damageAdd;
        _resolving = true;
        try
        {
            await PlayEffect(choiceContext, cardPlay);
        }
        finally
        {
            _resolving = false;
            _responseBoost = 1m;
            _damageBoost = 1m;
            _damageAdd = 0m;
        }

        // 3) 传参：重新入池（在响应之后，才能自续）
        if (IsParamSource && core != null && BuildParam() is { } newParam)
        {
            core.AddParam(newParam);
        }

        // 4) 迭代：记一次打出。层数 = 次数 ÷ 迭代间隔，从下一次打出起生效
        if (IterationEvery > 0 && core != null)
        {
            int before = core.GetIterationStacks(this, IterationEvery);
            core.AddPlay(this);

            // E3：迭代层数真的涨了才算「完成一次迭代」→ 计入遗物计数条 + 广播。
            // 只是「打了一次但没满一轮」不计数。
            if (core.GetIterationStacks(this, IterationEvery) > before)
            {
                await OnIterationCompleted(choiceContext);
                core.BumpResponseOrIteration();
                await AlphaHooks.IterationCompleted(choiceContext, this);
            }
        }
    }

    /// <summary>
    /// 本牌「伤害」的原始值（力量 / 活力还没加）= floor((基础值 + 迭代成长) × 本次增幅)。
    /// 实战（<see cref="PlayEffect"/> 期间）用本次吃到的传参增幅，预览用池里挂着的增幅（见 <see cref="PreviewBoostFor"/>）。
    /// 卡面（<see cref="AlphaDamageVar"/>）与实战（<c>DamageCmd.Attack</c>）都把这个原始值交给
    /// <c>Hook.ModifyDamage</c>，所以力量 / 活力在两边加得完全一致，不再出现「卡面 60、实打 10」。
    /// </summary>
    internal decimal RawDamage()
    {
        decimal boost = _resolving ? _responseBoost * _damageBoost : PreviewBoostFor("Damage");
        decimal raw = ApplyBoost(RawBase("Damage"), boost);
        // DamageAdd 是「乘算增幅之后再叠加」的平伤：先乘后加。
        return raw + (_resolving ? _damageAdd : PreviewDamageAddFor());
    }

    /// <summary>卡面预览用的平伤加成 = 池里挂着的 DamageAdd 之和（只有响应牌才算）。</summary>
    private decimal PreviewDamageAddFor() =>
        IsResponse ? AlphaCorePower.Of(this)?.ParamDamageAdd ?? 0m : 0m;

    /// <summary>本牌「格挡」的原始值，口径同 <see cref="RawDamage"/>（α协议增幅对格挡也生效）。</summary>
    internal decimal RawBlock()
    {
        decimal boost = _resolving ? _responseBoost : PreviewBoostFor("Block");
        return ApplyBoost(RawBase("Block"), boost);
    }

    /// <summary>动态变量基础值 + 迭代成长（还没乘增幅）。</summary>
    private decimal RawBase(string varName) =>
        (DynamicVars.TryGetValue(varName, out DynamicVar? variable) ? variable.BaseValue : 0m)
        + IterationBonusFor(varName);

    private static decimal ApplyBoost(decimal raw, decimal boost) =>
        boost == 1m ? raw : Math.Floor(raw * boost);

    /// <summary>
    /// 非伤害 / 格挡的自定义变量（如协议层数）的结算值 =（基础值 + 迭代加成）× 本次增幅，向下取整。
    /// 这些变量没有力量 / 活力之类的引擎钩子，预览由 <see cref="AlphaPreviewPatch"/> 补同样的加成。
    /// </summary>
    protected decimal ResolveValue(string varName, decimal iterationBonus = 0m)
    {
        decimal raw = (DynamicVars.TryGetValue(varName, out DynamicVar? variable)
            ? variable.BaseValue
            : 0m) + iterationBonus;
        return ApplyBoost(raw, _responseBoost);
    }

    /// <summary>
    /// 「协议消耗牌」用：消耗至多 <paramref name="max"/> 层协议，返回**实际消耗**的层数（E8）。
    /// 协议不足时返回 0 或更少，**不做门禁**——牌照常打出，只是走弱效果。
    /// </summary>
    protected int ConsumeProtocol(int max) => ProtocolPower.Of(this)?.ConsumeLayers(max) ?? 0;

    /// <summary>当前协议层数（只读，供卡面/条件判断）。</summary>
    protected int CurrentProtocol => ProtocolPower.Of(this)?.Layers ?? 0;

    /// <summary>
    /// 卡面预览用：当前传参池挂着的增幅（α协议 +50% / 凝神聚气双倍伤害）。1 = 无增幅。
    /// 只有响应牌才算——池里的增幅会在打出时作用在这张牌上。
    /// </summary>
    public decimal PreviewBoostFor(string varName)
    {
        if (!IsResponse)
        {
            return 1m;
        }
        AlphaCorePower? core = AlphaCorePower.Of(this);
        if (core == null)
        {
            return 1m;
        }
        decimal boost = core.ParamBoost;
        if (varName == "Damage")
        {
            boost *= core.ParamDamageBoost;
        }
        return boost;
    }

    /// <summary>本牌自己的效果。</summary>
    protected abstract Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay);

    /// <summary>打出时加入传参池的条目；不带传参返回 null。</summary>
    protected virtual ParamEntry? BuildParam() => null;

    /// <summary>结算一条传参。</summary>
    protected virtual async Task ApplyParam(PlayerChoiceContext choiceContext, CardPlay cardPlay,
        ParamEntry entry)
    {
        switch (entry.Kind)
        {
            case ParamKind.Draw:
                await CardPileCmd.Draw(choiceContext, (int)entry.Value, Owner);
                break;

            case ParamKind.Heal:
                await CreatureCmd.Heal(Owner.Creature, entry.Value);
                break;

            case ParamKind.Replay:
                BaseReplayCount += (int)entry.Value;
                break;

            case ParamKind.GainProtocol:
                ProtocolPower.Of(this)?.AddProtocol((int)entry.Value);
                break;

            case ParamKind.Debuff:
                await ApplyDebuff(choiceContext, cardPlay, entry);
                break;

            case ParamKind.Keyword:
                AddKeyword((CardKeyword)entry.Variant);
                break;

            case ParamKind.GenerateCard:
                await GenerateCard(choiceContext, entry);
                break;

            case ParamKind.KeywordAllHand:
                foreach (CardModel handCard in PileType.Hand.GetPile(Owner).Cards)
                {
                    handCard.AddKeyword((CardKeyword)entry.Variant);
                }
                break;

            case ParamKind.ChooseClassCard:
                await GenerateClassCardChoice(choiceContext, freeThisTurn: false);
                break;
        }
    }

    /// <summary>
    /// 负面状态作用在这张牌本次的目标上。单体牌用 cardPlay.Target；
    /// 全体指向的牌没有单一目标，退回按卡牌自身的指向取（如「对所有敌人」）。
    /// </summary>
    private async Task ApplyDebuff(PlayerChoiceContext choiceContext, CardPlay cardPlay,
        ParamEntry entry)
    {
        var targets = new List<Creature>();
        if (cardPlay.Target is { } single)
        {
            targets.Add(single);
        }
        else
        {
            targets.AddRange(this.GetTargets());
        }
        if (targets.Count == 0)
        {
            return;
        }

        foreach (Creature target in targets)
        {
            switch ((DebuffKind)entry.Variant)
            {
                case DebuffKind.Vulnerable:
                    await PowerCmd.Apply<VulnerablePower>(choiceContext, target, entry.Value,
                        Owner.Creature, this);
                    break;
                case DebuffKind.Weak:
                    await PowerCmd.Apply<WeakPower>(choiceContext, target, entry.Value,
                        Owner.Creature, this);
                    break;
                case DebuffKind.Frail:
                    await PowerCmd.Apply<FrailPower>(choiceContext, target, entry.Value,
                        Owner.Creature, this);
                    break;
                case DebuffKind.Poison:
                    await PowerCmd.Apply<PoisonPower>(choiceContext, target, entry.Value,
                        Owner.Creature, this);
                    break;
            }
        }
    }

    private async Task GenerateCard(PlayerChoiceContext choiceContext, ParamEntry entry)
    {
        ICombatState? combatState = Owner.Creature.CombatState;
        if (combatState == null)
        {
            return;
        }

        switch ((GenCardKind)entry.Variant)
        {
            // 小刀：用本体的生成入口，按 Value 张数一次进手（E7）。
            case GenCardKind.Shiv:
                await Shiv.CreateInHand(Owner, Math.Max(1, (int)entry.Value), combatState, Owner);
                return;

            // 随机合法附魔的小刀（E10）。
            case GenCardKind.EnchantedShiv:
                await GenerateEnchantedShivs(choiceContext, Math.Max(1, (int)entry.Value));
                return;

            // 随机无色牌（E13）。
            case GenCardKind.ColorlessRandom:
                await GenerateColorless(choiceContext, Math.Max(1, (int)entry.Value));
                return;
        }

        CardModel card = (GenCardKind)entry.Variant switch
        {
            GenCardKind.ProtocolAlpha => combatState.CreateCard<ProtocolAlpha>(Owner),
            GenCardKind.ProtocolBeta => combatState.CreateCard<ProtocolBeta>(Owner),
            GenCardKind.ProtocolGamma => combatState.CreateCard<ProtocolGamma>(Owner),
            _ => combatState.CreateCard(CanonicalInstance, Owner),
        };

        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
    }

    /// <summary>生成 N 张随机合法附魔的小刀加入手牌（E10）。</summary>
    protected async Task GenerateEnchantedShivs(PlayerChoiceContext choiceContext, int count)
    {
        if (count <= 0)
        {
            return;
        }
        ICombatState? combatState = Owner.Creature.CombatState;
        if (combatState == null)
        {
            return;
        }
        IEnumerable<CardModel> shivs = await Shiv.CreateInHand(Owner, count, combatState, Owner);
        foreach (CardModel shiv in shivs)
        {
            EnchantmentModel? pick = RandomLegalEnchantment(shiv);
            if (pick != null)
            {
                CardCmd.Enchant(pick, shiv, 1m);
            }
        }
    }

    /// <summary>随机挑一个对该牌合法的附魔（E10）；没有合法附魔时返回 null。</summary>
    protected EnchantmentModel? RandomLegalEnchantment(CardModel card)
    {
        List<EnchantmentModel> legal = ModelDb.DebugEnchantments
            .Where(e => e.GetType().Name is not ("DeprecatedEnchantment" or "MockFreeEnchantment"))
            .Where(e => e.CanEnchant(card))
            .ToList();
        if (legal.Count == 0)
        {
            return null;
        }
        var rng = Owner.RunState.Rng.CombatCardGeneration;
        return legal[rng.NextInt(legal.Count)].ToMutable();
    }

    /// <summary>生成 N 张随机无色牌加入手牌（E13）。</summary>
    protected async Task GenerateColorless(PlayerChoiceContext choiceContext, int count)
    {
        if (count <= 0)
        {
            return;
        }
        IEnumerable<CardModel> cards = CardFactory.GetDistinctForCombat(
            Owner,
            ModelDb.CardPool<ColorlessCardPool>()
                .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint),
            count,
            Owner.RunState.Rng.CombatCardGeneration);
        foreach (CardModel card in cards.ToList())
        {
            await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
        }
    }

    /// <summary>从 3 张本职业随机牌里选 1 张加入手牌（E12）；freeThisTurn = 本回合可免费打出。</summary>
    protected async Task GenerateClassCardChoice(PlayerChoiceContext choiceContext, bool freeThisTurn)
    {
        List<CardModel> choices = CardFactory.GetDistinctForCombat(
            Owner,
            Owner.Character.CardPool
                .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
                .Where(c => c is not AdvanceFuture),
            3,
            Owner.RunState.Rng.CombatCardGeneration).ToList();
        if (choices.Count == 0)
        {
            return;
        }
        CardModel? picked = await CardSelectCmd.FromChooseACardScreen(choiceContext, choices, Owner, canSkip: true);
        if (picked == null)
        {
            return;
        }
        await CardPileCmd.AddGeneratedCardToCombat(picked, PileType.Hand, Owner);
        if (freeThisTurn)
        {
            picked.SetToFreeThisTurn();
        }
    }
}