using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Patches.Localization;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;

namespace MyCharMod.Mechanics;

/// <summary>
/// 阿尔法机的战斗内核心状态：传参池 / 迭代计数 / 完全响应保留 / 自我升级计数。
///
/// 做成**可见 Power**：状态栏上显示池中传参的条数，悬浮能逐条看到池里有什么。
/// 层数放在 <see cref="PowerModel.GetInternalData{T}"/> 里，不走 Amount——因为 Amount 掉到 0
/// 时引擎会把 Power 整个移除，那样状态栏上的图标就会消失。
///
/// 生命周期由引擎托管：战斗开始时由起始遗物（自我升级）挂上，战斗结束随 Power 一起消失。
/// </summary>
public sealed class AlphaCorePower : CustomPowerModel, IAddDumbVariablesToPowerDescription
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override bool ShouldScaleInMultiplayer => false;

    public override string? CustomPackedIconPath => MyCharAssets.Power("param_pool");

    public override string? CustomBigIconPath => MyCharAssets.Power("param_pool");

    public override List<(string, string)>? Localization => new()
    {
        ("title", "传参池"),
        // 只列池子里有什么。机制解释交给卡面关键词，不在这里重复。
        ("description", "{Pool}"),
    };

    /// <summary>战斗内可变状态。</summary>
    public sealed class Data
    {
        /// <summary>传参池。</summary>
        public readonly List<ParamEntry> Params = new();

        /// <summary>本场战斗内每张牌已打出的次数。迭代层数 = 次数 ÷ 迭代间隔 X。</summary>
        public readonly Dictionary<CardModel, int> Plays = new();

        /// <summary>每张牌「已经结算过」的迭代完成档数，用来补结算外部推进的计数（见 AdvanceIteration）。</summary>
        public readonly Dictionary<CardModel, int> FiredIterations = new();

        /// <summary>完全响应：已经挂在这张牌上、之后每次打出都重放的传参。</summary>
        public readonly Dictionary<CardModel, List<ParamEntry>> Retained = new();

        public int ResponseOrIterationCount;

        /// <summary>「自我升级」已经触发过的里程碑数（每 3 次响应/迭代一次）。</summary>
        public int Milestones;

        /// <summary>最近一次响应消费掉的传参（供「完全形态」随机返回一条）。</summary>
        public readonly List<ParamEntry> LastConsumed = new();

        /// <summary>本场战斗打出的牌总数（供「零基启动」的迭代加成读取）。</summary>
        public int TotalPlays;

        /// <summary>
        /// 动态迭代加成：迭代完成时按当时的「本场已打出牌数」快照累计出来的额外数值
        /// （「零基启动」的伤害加成走这里，而不是实时的 TotalPlays）。
        /// </summary>
        public readonly Dictionary<CardModel, decimal> DynamicIterationBonus = new();

        // ————————————— 遗物用的战斗内 / 回合内状态（P2） —————————————

        /// <summary>「预协议」本场战斗是否已发过 4 层协议。</summary>
        public bool PreProtocolGranted;

        /// <summary>「加速器」本回合是否已领过「首次完成迭代」的能量。</summary>
        public bool AcceleratorUsedThisTurn;

        /// <summary>「共演」本回合是否打出过迭代牌。</summary>
        public bool IterationPlayedThisTurn;

        /// <summary>「欧米伽协议」本场战斗已触发过（集齐 α/β/γ）后的伤害。</summary>
        public bool OmegaTriggered;

        /// <summary>「欧米伽协议」本场战斗已打出过的协议种类（"alpha"/"beta"/"gamma"），集齐三种才触发。</summary>
        public readonly HashSet<string> PlayedProtocols = new();
    }

    protected override object? InitInternalData() => new Data();

    public Data Core => GetInternalData<Data>();

    /// <summary>状态栏上的数字 = 池中传参条数。</summary>
    public override int DisplayAmount => Core.Params.Count;

    /// <summary>把池子的内容塞进 tooltip 的 {Pool} 占位符。</summary>
    public void AddDumbVariablesToPowerDescription(LocString description)
    {
        description.Add("Pool", PoolText());
    }

    private string PoolText()
    {
        if (Core.Params.Count == 0)
        {
            return "（空）";
        }
        // 同类（种类 / 数值 / 子类型都相同）合并成「描述 ×N」，减少状态栏行数。
        // 仅影响显示：池中条目与触发顺序完全不变，每条仍独立结算。
        IEnumerable<string> lines = Core.Params
            .GroupBy(entry => (entry.Kind, entry.Value, entry.Variant))
            .Select(group => group.Count() == 1
                ? group.First().Describe()
                : $"{group.First().Describe()} ×{group.Count()}");
        return string.Join("\n", lines);
    }

    // ————————————————— 传参池 —————————————————

    /// <summary>本场战斗打出的牌总数（含本体基础牌）。</summary>
    public int TotalPlays => Core.TotalPlays;

    /// <summary>每打出一张自己的牌就记一笔（供「零基启动」读取）。</summary>
    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Player == Owner.Player)
        {
            Core.TotalPlays++;
        }
        return Task.CompletedTask;
    }

    public bool HasParams => Core.Params.Count > 0;

    /// <summary>池中传参条数（「评估」「过载状态」等按条数结算的牌读取用）。</summary>
    public int ParamCount => Core.Params.Count;

    /// <summary>池中 EffectBoost 传参叠加后的总增幅（1 = 无增幅）。供卡面预览与结算共用。</summary>
    public decimal ParamBoost
    {
        get
        {
            decimal boost = 1m;
            foreach (ParamEntry entry in Core.Params)
            {
                if (entry.Kind == ParamKind.EffectBoost)
                {
                    boost += entry.Value;
                }
            }
            return boost;
        }
    }

    /// <summary>池中 DamageBoost 传参叠加后的伤害总增幅（1 = 无增幅）。只作用于伤害。</summary>
    public decimal ParamDamageBoost
    {
        get
        {
            decimal boost = 1m;
            foreach (ParamEntry entry in Core.Params)
            {
                if (entry.Kind == ParamKind.DamageBoost)
                {
                    boost += entry.Value;
                }
            }
            return boost;
        }
    }

    /// <summary>池中 DamageAdd 传参叠加后的**平伤**加成（乘算增幅之后再加，见 <see cref="AlphaCard.RawDamage"/>）。</summary>
    public decimal ParamDamageAdd
    {
        get
        {
            decimal add = 0m;
            foreach (ParamEntry entry in Core.Params)
            {
                if (entry.Kind == ParamKind.DamageAdd)
                {
                    add += entry.Value;
                }
            }
            return add;
        }
    }

    public void AddParam(ParamEntry entry)
    {
        Core.Params.Add(entry);
        InvokeDisplayAmountChanged();
    }

    /// <summary>
    /// 入池并广播「传参入池」事件（E18，供「终响激发」监听）。
    /// 所有入池入口都应走这里，否则监听者会漏触发。
    /// </summary>
    public async Task AddParamWithHook(PlayerChoiceContext choiceContext, ParamEntry entry)
    {
        AddParam(entry);
        if (Owner.Player is { } player)
        {
            await AlphaHooks.ParamAdded(choiceContext, player, entry);
        }
    }

    /// <summary>取走并清空池中全部传参。</summary>
    public List<ParamEntry> ConsumeParams()
    {
        var taken = new List<ParamEntry>(Core.Params);
        Core.Params.Clear();
        Core.LastConsumed.Clear();
        Core.LastConsumed.AddRange(taken);
        InvokeDisplayAmountChanged();
        return taken;
    }

    /// <summary>最近一次响应消费掉的传参（只读快照，供「完全形态」随机返回一条）。</summary>
    public IReadOnlyList<ParamEntry> LastConsumed => Core.LastConsumed;

    /// <summary>只读快照（用于「不消耗传参」的响应，如继承迭代）。</summary>
    public List<ParamEntry> PeekParams() => new(Core.Params);

    // ————————————————— 迭代 —————————————————

    /// <summary>本场战斗中这张牌已经打出的次数。</summary>
    public int GetPlayCount(CardModel card) => Core.Plays.TryGetValue(card, out int n) ? n : 0;

    /// <summary>迭代已叠加的层数 = 已打出次数 ÷ 迭代间隔 X。</summary>
    public int GetIterationStacks(CardModel card, int every) =>
        every <= 0 ? 0 : GetPlayCount(card) / every;

    /// <summary>记一次打出。</summary>
    public void AddPlay(CardModel card) => Core.Plays[card] = GetPlayCount(card) + 1;

    /// <summary>动态迭代加成（迭代完成时累计的额外数值）。</summary>
    public decimal GetDynamicIterationBonus(CardModel card) =>
        Core.DynamicIterationBonus.TryGetValue(card, out decimal value) ? value : 0m;

    public void AddDynamicIterationBonus(CardModel card, decimal amount) =>
        Core.DynamicIterationBonus[card] = GetDynamicIterationBonus(card) + amount;

    /// <summary>
    /// 直接增减某张牌的「已打出次数」，用于改迭代计数（E4）。不会低于 0。
    /// 计数走「已打出次数」而不是「层数」，所以迭代间隔 X &gt; 1 时，+1 只是推进一格进度。
    /// </summary>
    public void AddPlayCount(CardModel card, int amount)
    {
        if (amount == 0)
        {
            return;
        }
        Core.Plays[card] = Math.Max(0, GetPlayCount(card) + amount);
    }

    /// <summary>这张牌已经结算过的迭代完成档数。</summary>
    public int GetFiredStacks(CardModel card) =>
        Core.FiredIterations.TryGetValue(card, out int n) ? n : 0;

    /// <summary>记下这张牌已经结算到第几档迭代完成。</summary>
    public void SetFiredStacks(CardModel card, int value) => Core.FiredIterations[card] = value;

    /// <summary>
    /// 推进某张牌的迭代计数，并**立刻补结算**它跨过的每一档迭代完成。
    /// 供「加速编译 / 快速迭代 / 共演」等外部改计数的牌与能力调用——
    /// 否则这些牌跨过的档位不会被结算，表现为迭代效果时有时无。
    /// </summary>
    public async Task AdvanceIteration(PlayerChoiceContext choiceContext, CardModel card, int amount)
    {
        AddPlayCount(card, amount);
        if (card is AlphaCard alpha)
        {
            await alpha.FirePendingIterations(choiceContext);
        }
        RefreshCardFace(card);
    }

    /// <summary>
    /// 计数变化后刷新卡面。手牌里的牌节点会一直挂着，不会自己重算 <c>{IterCount}</c>，
    /// 不刷新就会出现「抽牌堆里的牌（重新抽到时重算）已 +1，手牌里那张还停在旧数字」。
    /// 抽牌堆 / 弃牌堆 / 消耗堆没有常驻节点（<see cref="NCard.FindOnTable"/> 对它们返回 null），
    /// 它们的卡面在打开牌堆界面时按当前计数重算，无需处理。
    /// </summary>
    private static void RefreshCardFace(CardModel card)
    {
        if (card.Pile is not { } pile)
        {
            return;
        }
        NCard.FindOnTable(card)?.UpdateVisuals(pile.Type, CardPreviewMode.Normal);
    }

    // ————————————————— 完全响应：把传参挂到牌上 —————————————————

    public IReadOnlyList<ParamEntry> GetRetained(CardModel card) =>
        Core.Retained.TryGetValue(card, out List<ParamEntry>? list)
            ? list
            : Array.Empty<ParamEntry>();

    public void AddRetained(CardModel card, IEnumerable<ParamEntry> entries)
    {
        if (!Core.Retained.TryGetValue(card, out List<ParamEntry>? list))
        {
            Core.Retained[card] = list = new List<ParamEntry>();
        }
        list.AddRange(entries);
    }

    // ————————————————— 起始遗物「自我升级」 —————————————————

    /// <summary>计数条进度：响应/迭代次数对 3 取余，显示 0 / 1 / 2（原版计数条样式）。</summary>
    public int CounterProgress => Core.ResponseOrIterationCount % 3;

    /// <summary>
    /// 记一次「响应或迭代」。每满 3 次，按遗物给的层数往协议层数里写入
    /// （普通「自我升级」1 层 / 古老版 2 层，见 <see cref="IAlphaProtocolMilestone"/>）。
    /// </summary>
    public void BumpResponseOrIteration()
    {
        Core.ResponseOrIterationCount++;

        Player? player = Owner.Player;
        if (player == null)
        {
            return;
        }

        IAlphaProtocolMilestone? milestone = player.Relics
            .OfType<IAlphaProtocolMilestone>()
            .FirstOrDefault();
        milestone?.NotifyCounterChanged();

        int milestones = Core.ResponseOrIterationCount / 3;
        if (milestones <= Core.Milestones)
        {
            return;
        }
        Core.Milestones = milestones;

        ProtocolPower.Of(player)?.AddProtocol(milestone?.ProtocolPerMilestone ?? 1);
    }

    // ————————————————— 查找 —————————————————

    public static AlphaCorePower? Of(CardModel card) =>
        card.IsMutable ? card.Owner?.Creature?.GetPower<AlphaCorePower>() : null;

    public static AlphaCorePower? Of(Player player) =>
        player.Creature?.GetPower<AlphaCorePower>();
}