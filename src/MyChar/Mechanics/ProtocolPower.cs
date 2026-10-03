using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
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
public sealed class ProtocolPower : CustomPowerModel
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
        // 状态栏数字已显示当前层数，tooltip 不复述 α/β/γ 档位表（各档协议牌自带卡面说明）。
        ("description", "回合开始时消耗全部层数，按档位将一张协议牌加入手牌。"),
    };

    /// <summary>档位上限：1-4 → α，5-10 → β，11 → γ，超过 γ 的部分转活力。</summary>
    private const int GammaTier = 11;

    private sealed class Data
    {
        public int Layers;
    }

    protected override object? InitInternalData() => new Data();

    private Data Core => GetInternalData<Data>();

    /// <summary>当前层数。</summary>
    public int Layers => Core.Layers;

    /// <summary>状态栏上的数字。</summary>
    public override int DisplayAmount => Core.Layers;

    public void AddProtocol(int layers)
    {
        if (layers == 0)
        {
            return;
        }
        // 「协议共振」：正向获取时额外追加 Amount 层。只认正向获取，消耗/扣减路径不受影响。
        int bonus = layers > 0 ? Owner.GetPower<ProtocolResonancePower>()?.Amount ?? 0 : 0;
        Core.Layers += layers + bonus;
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

    /// <summary>
    /// 回合开始时消费全部协议层数，按档位往手牌加入一张协议牌。
    /// 0 / 1-4 / 5-10 / 11+ 对应 无 / α / β / γ。
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
        if (layers <= 0)
        {
            return;
        }

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
            <= 10 => combatState.CreateCard<ProtocolBeta>(player),
            _ => combatState.CreateCard<ProtocolGamma>(player),
        };

        await CardPileCmd.AddGeneratedCardToCombat(proto, PileType.Hand, player);
    }
}