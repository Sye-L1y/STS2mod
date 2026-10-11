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
using MegaCrit.Sts2.Core.ValueProps;

namespace MyCharMod.Mechanics;

/// <summary>迭代效果的动态显示类型（见 docs/12 §1）。</summary>
public enum IterationEffectKind
{
    /// <summary>不注入 <c>{IterEffect}</c>：卡面手写迭代行。</summary>
    None,

    /// <summary>数值型（叠层）：层数累加进同一次施加。「给予1层易伤」→ 层数2 = 「给予2层易伤一次」。</summary>
    Numeric,

    /// <summary>次数型（叠次）：按层数分开施加。「给予1层易伤一次」→ 层数2 = 「给予1层易伤2次」。</summary>
    Count,

    /// <summary>获得型：达到档位后每次打出按层数结算。「获得1点能量」→ 层数2 = 「获得2点能量」。</summary>
    Gain,
}

/// <summary>
/// 迭代效果声明：前缀 + 每层数值 + 单位（单位可含 <c>[gold]</c> 标记，如「层[gold]易伤[/gold]」）。
/// 卡面用 <c>{IterEffect}</c> 引用，按当前层数解析成实际文案（见 <see cref="AlphaCard.ResolveIterationEffect"/>）。
/// </summary>
public readonly record struct IterationEffectSpec(string Prefix, decimal Value, string Unit);

/// <summary>
/// 阿尔法所有卡牌的基类，把四个核心机制的通用流程收在一处：
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

    /// <summary>本牌本次结算的增幅（α协议）。1 = 无增幅，打完还原。用于格挡 / 自定义变量。</summary>
    private decimal _responseBoost = 1m;

    /// <summary>本牌本次结算的**伤害侧**增幅：α协议 EffectBoost 的伤害部分，受伤害类传参倍率放大。只作用于 Damage。</summary>
    private decimal _responseDamageBoost = 1m;

    /// <summary>本牌本次结算的伤害专用增幅（凝神聚气）。只作用于 Damage，不动 Block。</summary>
    private decimal _damageBoost = 1m;

    /// <summary>本牌本次结算的**平伤**加成（传参 DamageAdd）。乘算增幅之后再叠加，打完还原。</summary>
    private decimal _damageAdd = 0m;

    /// <summary>是否正在结算本牌自身效果。用来区分「实战」（用本次吃到的增幅）与「预览」（用池里挂着的增幅）。</summary>
    private bool _resolving;

    /// <summary>
    /// 本牌本次结算消费到了「回手」传参（<see cref="ParamKind.ReturnToHand"/>）：
    /// 打完在 <see cref="AfterCardPlayedLate"/> 里回到手牌。一次性，只对本次打出生效。
    /// </summary>
    private bool _returnToHandAfterPlay;

    /// <summary>
    /// 本牌本次结算消费到了「受伤回格挡」传参（<see cref="ParamKind.BlockFromDamage"/>）：
    /// 在 <see cref="AfterDamageGiven"/> 里累加本牌造成的伤害，打完一次性换成等量格挡。
    /// </summary>
    private bool _gainBlockFromDamage;

    /// <summary>本牌本次结算已造成的伤害合计（供「攻守兼备」传参换算格挡）。</summary>
    private decimal _damageDealtThisPlay;

    /// <summary>本次打出时，响应是否真的消费到了传参（供「零基启动」判断是否回手）。每次打出刷新。</summary>
    protected bool ResponseConsumed { get; private set; }

    /// <summary>
    /// 本牌是否由传参生成（衍生的协议牌）。由「协议核心」生成的协议牌**不再触发**协议核心，
    /// 避免「打出协议 → 传参生成同款 → 再打出」无限连锁。
    /// </summary>
    public bool GeneratedByParam { get; private set; }

    /// <summary>标记本牌由传参生成（生成入口调用）。</summary>
    public void MarkGeneratedByParam() => GeneratedByParam = true;

    protected AlphaCard(int baseCost, CardType type, CardRarity rarity, TargetType target,
        bool showInCardLibrary = true)
        : base(baseCost, type, rarity, target, showInCardLibrary)
    {
    }

    /// <summary>是否**天生**带「响应」：打出时消费传参池。池空也能打出，只是没有加成。</summary>
    public virtual bool IsResponse => false;

    /// <summary>
    /// 本场战斗里由「语义附加」临时授予的「响应」（不写回卡牌本体：战斗结束牌回牌组即失效）。
    /// </summary>
    private bool _grantedResponse;

    /// <summary>
    /// **实际**是否带「响应」= 天生带（<see cref="IsResponse"/>）或本场被「语义附加」授予。
    /// 所有机制判断（消费传参 / 关键词显示 / 卡面预览）都读这个，不要直接读 <see cref="IsResponse"/>。
    /// </summary>
    public bool HasResponse => IsResponse || _grantedResponse;

    /// <summary>「语义附加」用：让这张牌本场战斗内获得「响应」。</summary>
    public void GrantResponse() => _grantedResponse = true;

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

    /// <summary>迭代效果的动态显示类型（见 docs/12 §1）。声明了 <see cref="IterationEffectSpec"/> 才生效。</summary>
    public virtual IterationEffectKind IterationEffectKind => IterationEffectKind.None;

    /// <summary>
    /// 迭代效果声明（数值型 / 次数型 / 获得型）：卡面用 <c>{IterEffect}</c> 引用，随层数动态解析。
    /// 只影响显示；实际结算由 <see cref="PlayEffect"/> 自己算。
    /// </summary>
    public virtual IterationEffectSpec? IterationEffectSpec => null;

    /// <summary>是否在卡面挂上「协议」词条（会往协议层数里写牌的牌）。</summary>
    public virtual bool GrantsProtocol => false;

    /// <summary>
    /// 本牌响应传参里「伤害增加」效果的倍率。1 = 不放大（默认）；「双重译制」= 2（翻倍）。
    /// 放大范围：DamageBoost 百分比、DamageAdd 平伤，以及 α协议 EffectBoost 的**伤害部分**；
    /// 格挡（含 EffectBoost 的格挡部分）不受影响。
    /// </summary>
    protected virtual decimal DamageParamMultiplier => 1m;

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
            // 被「语义附加」授予响应的牌，卡面同样挂上「响应」词条。
            if (HasResponse)
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
            // 迭代间隔会随升级变化的牌，把 IterEvery 做成 DynamicVar（见各卡），好让 {IterEvery:diff()} 显示升级色；
            // 其余固定间隔的牌继续注入固定值。
            if (!DynamicVars.TryGetValue("IterEvery", out _))
            {
                description.Add("IterEvery", (decimal)IterationEvery);
            }
            if (IterationEffectSpec is { } effectSpec)
            {
                description.Add("IterEffect", ResolveIterationEffect(effectSpec));
            }
        }
        if (RetainsResponse)
        {
            description.Add("Retained", RetainedText());
        }
    }

    /// <summary>
    /// 把迭代效果声明解析成**当前层数**下的卡面文案：
    ///   数值型「给予1层易伤」   → 层数 2 = 「给予2层易伤一次」（层数累加进同一次施加）
    ///   次数型「给予1层易伤一次」→ 层数 2 = 「给予1层易伤2次」（按层数分开施加，次数用阿拉伯数字）
    ///   获得型「打出后获得1点能量」→ 层数 2 = 「打出后获得2点能量」（直接显示总数，方便玩家）
    /// 还没完成任何迭代（层数 0）时按 1 层显示，作为「潜力」描述。
    /// </summary>
    private string ResolveIterationEffect(IterationEffectSpec spec)
    {
        int stacks = Math.Max(IterationStacks, 1);
        return IterationEffectKind switch
        {
            IterationEffectKind.Numeric => $"{spec.Prefix}{FormatAmount(spec.Value * stacks)}{spec.Unit}一次",
            IterationEffectKind.Count => $"{spec.Prefix}{FormatAmount(spec.Value)}{spec.Unit}{stacks}次",
            IterationEffectKind.Gain => $"{spec.Prefix}{FormatAmount(spec.Value * stacks)}{spec.Unit}",
            _ => "",
        };
    }

    private static string FormatAmount(decimal value) => value.ToString("0.##");

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
        // 每次打出都清掉上一次的「受伤回格挡」进度。
        _gainBlockFromDamage = false;
        _damageDealtThisPlay = 0m;
        // 「群伤」传参延后到本牌自身效果之后再结算（原因见下方响应阶段的注释）。
        var deferredGroupDamage = new List<ParamEntry>();

        // 1) 响应（HasResponse 含「语义附加」临时授予的响应）
        if (HasResponse && core != null)
        {
            var active = new List<ParamEntry>();
            if (RetainsResponse)
            {
                active.AddRange(core.GetRetained(this));
            }

            // 「预输入」：池里真有传参要保时，这一次响应读而不取（池原样留给下一次）。
            bool freeResponse = ConsumesParams && core.HasParams && core.TakeNextResponseFree();
            List<ParamEntry> entries = ConsumesParams && !freeResponse
                ? core.ConsumeParams()
                : core.PeekParams();
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
                        damageBoost += entry.Value * DamageParamMultiplier;
                    }
                    else if (entry.Kind == ParamKind.DamageAdd)
                    {
                        damageAdd += entry.Value * DamageParamMultiplier;
                    }
                }
                foreach (ParamEntry entry in active)
                {
                    if (entry.Kind is ParamKind.EffectBoost or ParamKind.DamageBoost
                        or ParamKind.DamageAdd)
                    {
                        continue;
                    }

                    // 「群伤」是本牌发起的一次强化攻击：若在响应阶段先结算，玩家身上的
                    // 活力会被它消耗掉（VigorPower 只认「下一次攻击」），本牌自身攻击就吃不到
                    // 卡面预览里已经算进去的活力——表现为卡面显示 29、本牌攻击实际只打 25。
                    // 延后到本牌自身效果之后再打，把活力留给本牌的攻击（群伤仍照常吃力量）。
                    if (entry.Kind == ParamKind.DamageAll)
                    {
                        deferredGroupDamage.Add(entry);
                        continue;
                    }

                    await ApplyParam(choiceContext, cardPlay, entry);
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
        // 伤害侧的 α 增幅受倍率放大（EffectBoost 的格挡部分留在 _responseBoost 里，不受影响）。
        _responseDamageBoost = 1m + (boost - 1m) * DamageParamMultiplier;
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
            _responseDamageBoost = 1m;
            _damageBoost = 1m;
            _damageAdd = 0m;
        }

        // 2.5) 群伤传参：延后到本牌自身攻击之后再结算（见响应阶段的说明）。
        foreach (ParamEntry entry in deferredGroupDamage)
        {
            await ApplyParam(choiceContext, cardPlay, entry);
        }

        // 2.6) 「攻守兼备」传参：本牌本次造成的伤害全部换算成格挡（无伤害则不给）。
        if (_gainBlockFromDamage)
        {
            _gainBlockFromDamage = false;
            if (_damageDealtThisPlay > 0m)
            {
                await CreatureCmd.GainBlock(Owner.Creature, _damageDealtThisPlay, ValueProp.Move, cardPlay);
            }
            _damageDealtThisPlay = 0m;
        }

        // 3) 传参：重新入池（在响应之后，才能自续）
        if (IsParamSource && core != null)
        {
            // 先材质化（BuildParams 可能读「最近一次入池批次」，必须先取好再开新批次），
            // 再开新批次并逐条入池——「二次输入」要重复上一次的传参就靠这个批次记录。
            List<ParamEntry> batch = BuildParams().ToList();
            core.BeginParamBatch();
            foreach (ParamEntry newParam in batch)
            {
                await core.AddParamWithHook(choiceContext, newParam);
            }
        }

        // 4) 迭代：记一次打出，并把「已达档位但还没结算过」的迭代完成全部补上。
        //    外部推进计数器（加速编译 / 快速迭代 / 共演…）跨过的档位也在下一次结算点补上，不再漏。
        if (IterationEvery > 0 && core != null)
        {
            core.AddPlay(this);
            await FirePendingIterations(choiceContext);
        }
    }

    /// <summary>
    /// 把这张牌「已经达到、但还没结算过」的迭代完成逐档补结算
    /// （自身迭代效果 + 遗物计数条 + 广播给监听者）。
    ///
    /// 完成与否按「已结算档数」判定，而不是「本次打出前后层数是否上涨」——
    /// 后者在别人替本牌推进计数器时会漏掉跨过的档位（表现为迭代效果时有时无）。
    /// </summary>
    internal async Task FirePendingIterations(PlayerChoiceContext choiceContext)
    {
        AlphaCorePower? core = AlphaCorePower.Of(this);
        if (core == null || IterationEvery <= 0)
        {
            return;
        }

        int stacks = core.GetIterationStacks(this, IterationEvery);
        int fired = core.GetFiredStacks(this);
        while (fired < stacks)
        {
            fired++;
            core.SetFiredStacks(this, fired);
            await OnIterationCompleted(choiceContext);
            core.BumpResponseOrIteration();
            await AlphaHooks.IterationCompleted(choiceContext, this);
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
        decimal boost = _resolving ? _responseDamageBoost * _damageBoost : PreviewBoostFor("Damage");
        decimal raw = ApplyBoost(RawBase("Damage"), boost);
        // DamageAdd 是「乘算增幅之后再叠加」的平伤：先乘后加。
        return raw + (_resolving ? _damageAdd : PreviewDamageAddFor());
    }

    /// <summary>卡面预览用的平伤加成 = 池里挂着的 DamageAdd 之和（只有响应牌才算，受伤害类传参倍率放大）。</summary>
    private decimal PreviewDamageAddFor() =>
        HasResponse ? (AlphaCorePower.Of(this)?.ParamDamageAdd ?? 0m) * DamageParamMultiplier : 0m;

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
    /// 「协议消耗牌」用：消耗至多 <paramref name="max"/> 层协议，返回**实际消耗**的层数（E8），
    /// 并广播「协议消耗」事件（E17）。
    /// 协议不足时返回 0 或更少，**不做门禁**——牌照常打出，只是走弱效果。
    /// </summary>
    protected async Task<int> ConsumeProtocol(PlayerChoiceContext choiceContext, int max)
    {
        ProtocolPower? protocol = ProtocolPower.Of(this);
        return protocol == null ? 0 : await protocol.ConsumeLayersWithBroadcast(choiceContext, max);
    }

    /// <summary>当前协议层数（只读，供卡面/条件判断）。</summary>
    protected int CurrentProtocol => ProtocolPower.Of(this)?.Layers ?? 0;

    /// <summary>
    /// 卡面预览用：当前传参池挂着的增幅（α协议 +50% / 凝神聚气双倍伤害）。1 = 无增幅。
    /// 只有响应牌才算——池里的增幅会在打出时作用在这张牌上。
    /// </summary>
    public decimal PreviewBoostFor(string varName)
    {
        if (!HasResponse)
        {
            return 1m;
        }
        AlphaCorePower? core = AlphaCorePower.Of(this);
        if (core == null)
        {
            return 1m;
        }
        decimal effectBoost = core.ParamBoost;
        if (varName != "Damage")
        {
            return effectBoost;
        }
        // 伤害侧：EffectBoost 的伤害部分与 DamageBoost 都受本牌倍率放大
        //（默认 1 原样，双重译制 2 翻倍）；EffectBoost 的格挡部分不受影响（见 RawBlock 仍用 _responseBoost）。
        decimal damageBoost = 1m + (effectBoost - 1m) * DamageParamMultiplier;
        damageBoost *= 1m + (core.ParamDamageBoost - 1m) * DamageParamMultiplier;
        return damageBoost;
    }

    /// <summary>本牌自己的效果。</summary>
    protected abstract Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay);

    /// <summary>打出时加入传参池的条目；不带传参返回 null。</summary>
    protected virtual ParamEntry? BuildParam() => null;

    /// <summary>
    /// 打出时加入传参池的全部条目（默认只产出 <see cref="BuildParam"/> 的那一条）。
    /// 一条牌要写成多条传参（如「双通道」的「抽1张」+「回复2生命」）时 override 它。
    /// </summary>
    protected virtual IEnumerable<ParamEntry> BuildParams()
    {
        if (BuildParam() is { } single)
        {
            yield return single;
        }
    }

    /// <summary>
    /// 本牌作为「传参来源」会抛出的条目（供「灵魂记忆」这类**不打出就触发其传参**的效果读取）。
    /// 默认与 <see cref="BuildParams"/> 一致。
    /// </summary>
    internal IEnumerable<ParamEntry> ParamsForTrigger() => BuildParams();

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

            case ParamKind.DamageAll:
                await ApplyGroupDamage(choiceContext, cardPlay, entry);
                break;

            case ParamKind.Debuff:
                await ApplyDebuff(choiceContext, cardPlay, entry);
                break;

            case ParamKind.DebuffAll:
                await ApplyDebuffAll(choiceContext, entry);
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
                // 传参版固定不免费（只有「预支未来」本牌升级后的自身效果才本回合免费）。
                await GenerateClassCardChoice(choiceContext, freeThisTurn: false);
                break;

            case ParamKind.ExhaustHand:
                // 「冗余清理」传参：选择任意张手牌直接消耗（可不选、可多选）。
                await ExhaustChosenHandCards(choiceContext);
                break;

            case ParamKind.FreePlayRareCard:
                // 「特别参数」药水传参：选 1 张本职业稀有牌免费打出并消耗。
                await GenerateFreePlayRareCard(choiceContext);
                break;

            case ParamKind.Gold:
                await PlayerCmd.GainGold(entry.Value, Owner);
                break;

            case ParamKind.GainBlock:
                await CreatureCmd.GainBlock(Owner.Creature, entry.Value, ValueProp.Move, cardPlay);
                break;

            case ParamKind.BlockFromDamage:
                // 没有即时效果：打标记，本牌本次造成的伤害在 AfterDamageGiven 里累加，
                // 打完一次性换算成等量格挡（见 OnPlay 的 2.6 步）。
                _gainBlockFromDamage = true;
                break;

            case ParamKind.ReturnToHand:
                // 没有即时效果：只打标记，打完在 AfterCardPlayedLate 里把自己捞回手牌。
                _returnToHandAfterPlay = true;
                break;
        }
    }

    /// <summary>
    /// 「攻守兼备」传参：本牌（响应牌）本次结算造成的伤害逐笔记账。
    /// 只记「本牌自己打出的、且正在结算中」的伤害（<c>cardSource == this &amp;&amp; _resolving</c>），
    /// 打完在 <see cref="OnPlay"/> 的 2.6 步一次性换成等量格挡。
    /// </summary>
    public override Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer,
        DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (_resolving && _gainBlockFromDamage && cardSource == this)
        {
            _damageDealtThisPlay += result.TotalDamage;
        }
        return base.AfterDamageGiven(choiceContext, dealer, result, props, target, cardSource);
    }

    /// <summary>
    /// 「回手」传参（<see cref="ParamKind.ReturnToHand"/>）：本次消费到它的牌**打完回到手牌**。
    /// 与本体「本回合打出的牌回到手牌」类牌同口径——在 <c>AfterCardPlayedLate</c> 里看自己是不是已经不在手牌，
    /// 不在就捞回来。只在「本牌自己被打出、且本次确实吃到回手传参」时生效，一次性。
    /// </summary>
    /// <summary>
    /// 打完一张牌之后的收尾（此处在引擎把打出的牌移出「出牌区」<b>之后</b>触发，捞回手牌必须在这个时机）：
    ///   ① 「回手」传参（<see cref="ParamKind.ReturnToHand"/>）：本次消费到它的牌回手，一次性。
    ///   ② 「调节」Power：本回合**第一次**打出[gold]迭代[/gold]牌时该牌回手（所有迭代牌共用一次额度）。
    /// 两者都只在「本牌自己被打出」时生效，且回手前都会先把标记消费掉（不会重复触发）。
    /// </summary>
    public override async Task AfterCardPlayedLate(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await base.AfterCardPlayedLate(choiceContext, cardPlay);
        if (cardPlay.Card != this)
        {
            return;
        }

        bool returnToHand = _returnToHandAfterPlay;
        _returnToHandAfterPlay = false;

        // 「调节」额度由 Power 记：本回合没用过才回手，用过就不再回（防「回手 → 再打出 → 再回手」死循环）。
        if (IterationEvery > 0 && AdjustmentPower.TryConsumeTurnSlot(Owner))
        {
            returnToHand = true;
        }

        if (returnToHand && Pile?.Type != PileType.Hand)
        {
            await CardPileCmd.Add(this, PileType.Hand);
        }
    }

    /// <summary>
    /// 负面状态作用在这张牌本次的**敌方**目标上。单体攻击牌用 cardPlay.Target；
    /// 群攻牌没有单一目标，退回按卡牌自身的指向取（如「对所有敌人」）。
    /// 指向自身 / 无目标的技能牌没有敌方目标——此时随机挑一个敌人，避免负面状态落到自己身上
    /// （Self 卡牌的 GetTargets() 会返回自己）。
    /// </summary>
    private async Task ApplyDebuff(PlayerChoiceContext choiceContext, CardPlay cardPlay,
        ParamEntry entry)
    {
        var targets = new List<Creature>();
        if (cardPlay.Target is { } single && IsEnemy(single))
        {
            targets.Add(single);
        }
        else
        {
            targets.AddRange(this.GetTargets().Where(IsEnemy));
        }

        if (targets.Count == 0 && RandomEnemy() is { } fallback)
        {
            targets.Add(fallback);
        }
        if (targets.Count == 0)
        {
            return;
        }

        foreach (Creature target in targets)
        {
            await ApplyDebuffTo(choiceContext, target, entry);
        }
    }

    /// <summary>是否为本场战斗中可命中的敌人。</summary>
    private bool IsEnemy(Creature creature) =>
        Owner.Creature.CombatState?.HittableEnemies.Contains(creature) ?? false;

    /// <summary>随机挑一个可命中的敌人；没有敌人时返回 null。</summary>
    protected Creature? RandomEnemy()
    {
        IReadOnlyList<Creature>? enemies = Owner.Creature.CombatState?.HittableEnemies;
        if (enemies == null || enemies.Count == 0)
        {
            return null;
        }
        return enemies[Owner.RunState.Rng.CombatTargets.NextInt(enemies.Count)];
    }

    /// <summary>「给全体敌人」传参：不跟随响应牌目标，直接对所有可命中敌人施加（规避自伤）。</summary>
    private async Task ApplyDebuffAll(PlayerChoiceContext choiceContext, ParamEntry entry)
    {
        ICombatState? combatState = Owner.Creature.CombatState;
        if (combatState == null)
        {
            return;
        }

        foreach (Creature target in combatState.HittableEnemies)
        {
            await ApplyDebuffTo(choiceContext, target, entry);
        }
    }

    /// <summary>「群伤」传参：响应牌结算时对所有敌人造成 Value 点伤害（不跟随响应牌目标）。</summary>
    private async Task ApplyGroupDamage(PlayerChoiceContext choiceContext, CardPlay cardPlay, ParamEntry entry)
    {
        ICombatState? combatState = CombatState ?? Owner.Creature.CombatState;
        if (combatState == null)
        {
            return;
        }

        await DamageCmd.Attack(entry.Value)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(combatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    private async Task ApplyDebuffTo(PlayerChoiceContext choiceContext, Creature target, ParamEntry entry)
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

            // 响应牌本身的复制（「镜像构建」）：CanonicalInstance = 正在结算的这张响应牌。
            case GenCardKind.CopyThisCard:
                for (int i = 0; i < Math.Max(1, (int)entry.Value); i++)
                {
                    CardModel copy = combatState.CreateCard(CanonicalInstance, Owner);
                    (copy as AlphaCard)?.MarkGeneratedByParam();
                    await CardPileCmd.AddGeneratedCardToCombat(copy, PileType.Hand, Owner);
                }
                return;
        }

        CardModel card = (GenCardKind)entry.Variant switch
        {
            GenCardKind.ProtocolAlpha => combatState.CreateCard<ProtocolAlpha>(Owner),
            GenCardKind.ProtocolBeta => combatState.CreateCard<ProtocolBeta>(Owner),
            GenCardKind.ProtocolGamma => combatState.CreateCard<ProtocolGamma>(Owner),
            _ => combatState.CreateCard(CanonicalInstance, Owner),
        };

        // 传参生成的牌打上标记：衍生的协议牌不再触发「协议核心」，避免无限连锁。
        (card as AlphaCard)?.MarkGeneratedByParam();
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
        // 「协议矩阵」在场时，新生成的协议牌自动升级。
        ProtocolMatrixPower.UpgradeNewProtocol(card, Owner);
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

    /// <summary>从 3 张本职业随机**稀有**牌里选 1 张加入手牌（E12）；freeThisTurn = 本回合可免费打出。</summary>
    protected async Task GenerateClassCardChoice(PlayerChoiceContext choiceContext, bool freeThisTurn)
    {
        List<CardModel> choices = CardFactory.GetDistinctForCombat(
            Owner,
            Owner.Character.CardPool
                .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
                .Where(c => c.Rarity == CardRarity.Rare && c is not AdvanceFuture),
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

    /// <summary>
    /// 「特别参数」传参：从 3 张本职业随机**稀有**牌里选 1 张，**免费打出并消耗**
    /// （<see cref="CardModel.ExhaustOnNextPlay"/> 保证打完直接进消耗堆）。
    /// </summary>
    private async Task GenerateFreePlayRareCard(PlayerChoiceContext choiceContext)
    {
        List<CardModel> choices = CardFactory.GetDistinctForCombat(
            Owner,
            Owner.Character.CardPool
                .GetUnlockedCards(Owner.UnlockState, Owner.RunState.CardMultiplayerConstraint)
                .Where(c => c.Rarity == CardRarity.Rare && c is not AdvanceFuture),
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

        picked.ExhaustOnNextPlay = true;
        await CardPileCmd.AddGeneratedCardToCombat(picked, PileType.Hand, Owner);
        await CardCmd.AutoPlay(choiceContext, picked, null);
    }

    /// <summary>
    /// 「冗余清理」传参：让玩家从手牌里选**任意张**直接消耗（min 0，可不选；max = 当前手牌数）。
    /// 用本体的「消耗」选牌提示，不依赖发起响应的那张牌的本地化键。
    /// </summary>
    private async Task ExhaustChosenHandCards(PlayerChoiceContext choiceContext)
    {
        List<CardModel> hand = PileType.Hand.GetPile(Owner).Cards.ToList();
        if (hand.Count == 0)
        {
            return;
        }

        IEnumerable<CardModel> picked = await CardSelectCmd.FromHand(choiceContext, Owner,
            new CardSelectorPrefs(CardSelectorPrefs.ExhaustSelectionPrompt, 0, hand.Count), null, this);

        foreach (CardModel card in picked.ToList())
        {
            await CardCmd.Exhaust(choiceContext, card);
        }
    }
}