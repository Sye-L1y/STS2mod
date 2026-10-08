using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Patches.Localization;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MyCharMod.Mechanics;

/// <summary>
/// 「协议」层数。做成**可见 Power**，状态栏上直接显示当前层数，悬浮能看到档位说明。
///
/// 生命周期由引擎托管：战斗开始时由起始遗物（自我升级）挂上，战斗结束随 Power 一起消失。
/// 层数放在 <see cref="PowerModel.GetInternalData{T}"/> 里，不走 Amount——因为 Amount 掉到 0
/// 时引擎会把 Power 整个移除，那样状态栏上的图标就会消失。
/// </summary>
public sealed class ProtocolPower : CustomPowerModel, IAddDumbVariablesToPowerDescription
{
    public override PowerType Type => PowerType.Buff;

    // Counter = 状态栏显示 DisplayAmount 数字（Single 会把数字藏起来，恒显示 1）。
    // 层数走内部数据、图标常驻，所以 0 层时也显示「0」而不是消失（见 docs/10 §6）。
    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldScaleInMultiplayer => false;

    public override string? CustomPackedIconPath => MyCharAssets.Power("protocol");

    public override string? CustomBigIconPath => MyCharAssets.Power("protocol");

    public override List<(string, string)>? Localization => new()
    {
        ("title", "协议"),
        // 状态栏数字显示当前层数；这里再补一句「当前层数 → 结算生成哪张协议牌」，方便实时对照档位。
        ("description", "当前{Layers}层，结算时生成{Tier}。回合开始时消耗全部层数，按档位将一张协议牌加入手牌（1-4→α协议，5-12→β协议，13+→γ协议，超出部分转活力）。"),
    };

    /// <summary>β 档上限：1-4 → α，5-12 → β。</summary>
    private const int BetaMax = 12;

    /// <summary>γ 档下限：13+ → γ；超出 13 层的部分结算时转等量活力。</summary>
    private const int GammaTier = 13;

    private sealed class Data
    {
        public int Layers;

        /// <summary>最近一次结算消耗掉的层数（供「备用协议」按消耗量的一半返还）。</summary>
        public int LastConsumed;

        /// <summary>本场战斗**累计获得过**的协议层数（含「协议共振」追加的部分，供「协议矩阵」里程碑读取）。</summary>
        public int TotalGained;
    }

    protected override object? InitInternalData() => new Data();

    private Data Core => GetInternalData<Data>();

    /// <summary>当前层数。</summary>
    public int Layers => Core.Layers;

    /// <summary>最近一次结算消耗掉的层数。</summary>
    public int LastConsumed => Core.LastConsumed;

    /// <summary>本场战斗累计获得过的协议层数（含「协议共振」追加部分）。</summary>
    public int TotalGained => Core.TotalGained;

    /// <summary>状态栏上的数字。</summary>
    public override int DisplayAmount => Core.Layers;

    /// <summary>状态栏描述里注入实时层数与当前档位对应的协议牌。</summary>
    public void AddDumbVariablesToPowerDescription(LocString description)
    {
        // 层数存在内部数据里，只有可变实例（战斗中的那张）才初始化过；canonical 实例取 0 兜底。
        int layers = IsMutable ? Core.Layers : 0;
        description.Add("Layers", layers);
        description.Add("Tier", TierName(layers));
    }

    /// <summary>当前层数结算时生成的协议牌档位文案。</summary>
    private static string TierName(int layers) => layers switch
    {
        <= 0 => "无",
        <= 4 => "[gold]α协议[/gold]",
        <= BetaMax => "[gold]β协议[/gold]",
        _ => "[gold]γ协议[/gold]",
    };

    public void AddProtocol(int layers)
    {
        if (layers == 0)
        {
            return;
        }
        // 「协议共振」：正向获取时额外追加 Amount 层。只认正向获取，消耗/扣减路径不受影响。
        int bonus = layers > 0 ? Owner.GetPower<ProtocolResonancePower>()?.Amount ?? 0 : 0;
        Core.Layers += layers + bonus;
        if (layers > 0)
        {
            Core.TotalGained += layers + bonus;
        }
        InvokeDisplayAmountChanged();
    }

    /// <summary>取走并清零全部层数。</summary>
    public int ConsumeProtocol()
    {
        int layers = Core.Layers;
        Core.Layers = 0;
        InvokeDisplayAmountChanged();
        return layers;
    }

    /// <summary>
    /// 消耗至多 <paramref name="max"/> 层，返回**实际消耗**的层数（E8）。
    /// 协议不足时不报错、不门禁——「协议消耗牌」协议不够就走弱效果，但仍能打出。
    /// </summary>
    public int ConsumeLayers(int max)
    {
        if (max <= 0 || Core.Layers <= 0)
        {
            return 0;
        }
        int taken = Core.Layers < max ? Core.Layers : max;
        Core.Layers -= taken;
        InvokeDisplayAmountChanged();
        return taken;
    }

    public static ProtocolPower? Of(CardModel card) =>
        card.IsMutable ? card.Owner?.Creature?.GetPower<ProtocolPower>() : null;

    public static ProtocolPower? Of(Player player) =>
        player.Creature?.GetPower<ProtocolPower>();

    /// <summary>消耗全部层数并广播「协议消耗」事件（供协议消耗牌调用）。返回实际消耗层数。</summary>
    public async Task<int> ConsumeAllWithBroadcast(PlayerChoiceContext choiceContext)
    {
        int layers = ConsumeProtocol();
        await BroadcastConsumed(choiceContext, layers);
        return layers;
    }

    /// <summary>消耗至多 <paramref name="max"/> 层并广播「协议消耗」事件（供协议消耗牌调用）。返回实际消耗层数。</summary>
    public async Task<int> ConsumeLayersWithBroadcast(PlayerChoiceContext choiceContext, int max)
    {
        int layers = ConsumeLayers(max);
        await BroadcastConsumed(choiceContext, layers);
        return layers;
    }

    private async Task BroadcastConsumed(PlayerChoiceContext choiceContext, int layers)
    {
        if (layers > 0 && Owner.Player is { } player)
        {
            await AlphaHooks.ProtocolConsumed(choiceContext, player, layers);
        }
    }

    /// <summary>
    /// 回合开始时消费全部协议层数，按档位往手牌加入一张协议牌。
    /// 0 / 1-4 / 5-12 / 13+ 对应 无 / α / β / γ。
    /// </summary>
    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner)
        {
            return;
        }

        await TriggerProtocol(choiceContext, player);
    }

    /// <summary>
    /// 消费全部层数并按档位往手牌加入一张协议牌。
    /// 供回合开始与「协议激活」（本回合立刻触发）共用。
    /// </summary>
    public async Task TriggerProtocol(PlayerChoiceContext choiceContext, Player player)
    {
        int layers = ConsumeProtocol();
        Core.LastConsumed = layers;
        if (layers <= 0)
        {
            return;
        }

        // 结算也属于「消耗协议」：广播给「厄难覆盖」等监听者。
        await BroadcastConsumed(choiceContext, layers);

        // 溢出兜底：超过 γ 档的层数转成等量活力，攒过头也不算白攒（见 docs/08 P2）。
        if (layers > GammaTier)
        {
            await PowerCmd.Apply<VigorPower>(choiceContext, player.Creature, layers - GammaTier,
                player.Creature, null);
        }

        // 必须走 combatState.CreateCard<T>(player)：它内部会 ToMutable + AddCard(owner) + AfterCreated。
        // 直接用 ModelDb.Card<T>().ToMutable() 得到的实例 Owner 为空，玩家打出时 PlayEffect 里
        // 访问 Owner 会抛异常，卡牌结算管线卡死（牌打不出、也不进弃牌堆）。
        ICombatState? combatState = player.Creature.CombatState;
        if (combatState == null)
        {
            return;
        }

        CardModel proto = layers switch
        {
            <= 4 => combatState.CreateCard<ProtocolAlpha>(player),
            <= BetaMax => combatState.CreateCard<ProtocolBeta>(player),
            _ => combatState.CreateCard<ProtocolGamma>(player),
        };

        await CardPileCmd.AddGeneratedCardToCombat(proto, PileType.Hand, player);
        // 「协议矩阵」在场时，新生成的协议牌自动升级。
        ProtocolMatrixPower.UpgradeNewProtocol(proto, player);
    }
}