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
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace MyCharMod.Mechanics;

/// <summary>
/// 「协议矩阵」的载体：
///   1. 本场战斗中所有 α/β/γ 协议牌**升级**（生成时自动升级，见 <see cref="UpgradeNewProtocol"/>）；
///   2. 本场战斗**首次累计获得 50 层协议**时，对所有敌人造成一次伤害，
///      伤害 = 当前**消耗堆**中每张 α/β/γ 协议牌各自提供的数值之和。
///
/// 每张协议牌提供多少伤害**只由「协议矩阵」这张牌是否升级决定**（基础 5/10/15，升级后 8/15/22，
/// 由卡牌打出时经 <see cref="Configure"/> 写入）；协议牌**自身**是否升级不影响该数值。
/// 里程碑按 <see cref="ProtocolPower.TotalGained"/>（累计获得量，含协议共振追加）判定。
/// </summary>
public sealed class ProtocolMatrixPower : CustomPowerModel, IAddDumbVariablesToPowerDescription
{
    /// <summary>触发群体伤害所需的累计获得协议层数。</summary>
    private const int Milestone = 50;

    private sealed class Data
    {
        /// <summary>本场战斗是否已触发过里程碑伤害。</summary>
        public bool Triggered;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override bool ShouldScaleInMultiplayer => false;

    public override string? CustomPackedIconPath => MyCharAssets.Power("protocol");

    public override string? CustomBigIconPath => MyCharAssets.Power("protocol");

    /// <summary>每张 α/β/γ 协议牌在消耗堆里提供的伤害；由卡牌打出时经 <see cref="Configure"/> 覆盖。</summary>
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("AlphaDamage", 5m),
        new DynamicVar("BetaDamage", 10m),
        new DynamicVar("GammaDamage", 15m),
    };

    public override List<(string, string)>? Localization => new()
    {
        ("title", "协议矩阵"),
        ("description", "本场战斗所有[gold]α/β/γ协议[/gold]牌升级。首次累计获得50层[gold]协议[/gold]时，按消耗堆中的协议牌造成一次群体伤害：每张α/β/γ协议牌分别提供{AlphaDamage}/{BetaDamage}/{GammaDamage}点伤害。"),
    };

    protected override object? InitInternalData() => new Data();

    private Data Core => GetInternalData<Data>();

    public void AddDumbVariablesToPowerDescription(LocString description)
    {
        description.Add("AlphaDamage", DynamicVars["AlphaDamage"].IntValue);
        description.Add("BetaDamage", DynamicVars["BetaDamage"].IntValue);
        description.Add("GammaDamage", DynamicVars["GammaDamage"].IntValue);
    }

    /// <summary>由「协议矩阵」卡牌在打出时写入每张协议牌提供的伤害（本牌升级后更高）。</summary>
    public void Configure(int alphaDamage, int betaDamage, int gammaDamage)
    {
        DynamicVars["AlphaDamage"].BaseValue = alphaDamage;
        DynamicVars["BetaDamage"].BaseValue = betaDamage;
        DynamicVars["GammaDamage"].BaseValue = gammaDamage;
    }

    /// <summary>是否为 α/β/γ 协议牌。</summary>
    public static bool IsProtocolCard(CardModel card) =>
        card is ProtocolAlpha or ProtocolBeta or ProtocolGamma;

    /// <summary>某张协议牌在消耗堆里提供的伤害；非协议牌返回 0。与协议牌自身是否升级无关。</summary>
    public int DamageOf(CardModel card) => card switch
    {
        ProtocolAlpha => DynamicVars["AlphaDamage"].IntValue,
        ProtocolBeta => DynamicVars["BetaDamage"].IntValue,
        ProtocolGamma => DynamicVars["GammaDamage"].IntValue,
        _ => 0,
    };

    /// <summary>若「协议矩阵」在场上，把新生成的 α/β/γ 协议牌升级（供各生成入口调用）。</summary>
    public static void UpgradeNewProtocol(CardModel card, Player player)
    {
        if (!IsProtocolCard(card) || card.IsUpgraded)
        {
            return;
        }
        if (player.Creature?.GetPower<ProtocolMatrixPower>() == null)
        {
            return;
        }
        CardCmd.Upgrade(card);
    }

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (cardPlay.Player != Owner.Player)
        {
            return;
        }
        await TryTrigger(choiceContext);
    }

    public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner)
        {
            return;
        }
        await TryTrigger(choiceContext);
    }

    private async Task TryTrigger(PlayerChoiceContext choiceContext)
    {
        if (Core.Triggered)
        {
            return;
        }
        Player? player = Owner.Player;
        if (player == null)
        {
            return;
        }
        ProtocolPower? protocol = ProtocolPower.Of(player);
        if (protocol == null || protocol.TotalGained < Milestone)
        {
            return;
        }

        int damage = 0;
        foreach (CardModel card in PileType.Exhaust.GetPile(player).Cards)
        {
            damage += DamageOf(card);
        }
        // 消耗堆里还没有协议牌时先不兑现，等有了再触发（避免白白消耗掉这次里程碑）。
        if (damage <= 0)
        {
            return;
        }
        Core.Triggered = true;

        ICombatState? combatState = Owner.CombatState;
        if (combatState == null)
        {
            return;
        }
        await CreatureCmd.Damage(choiceContext, combatState.HittableEnemies, damage,
            ValueProp.Unpowered, Owner);
    }
}