using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Patches.Localization;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;

namespace MyCharMod.Mechanics;

/// <summary>
/// 「调节」的载体：每回合**第一次**打出[gold]迭代[/gold]牌时，该牌回到手牌
/// （所有迭代牌**共用**这一次额度，见 docs/08 §0 规则 P4）。
///
/// 回手动作不在这里做——本 Power 只负责「额度」：<see cref="AlphaCard.AfterCardPlayedLate"/>
/// 在该牌打完（引擎已把它移出出牌区）之后来问一次 <see cref="TryConsumeTurnSlot"/>，
/// 拿到 true 才把它捞回手牌。额度每回合在 <see cref="AfterPlayerTurnStart"/> 刷新，
/// 因此「回手 → 再打出」不会无限循环。
///
/// **不叠**：<see cref="PowerStackType.Single"/>（开关型光环，有它就够）。
/// </summary>
public sealed class AdjustmentPower : CustomPowerModel, IAddDumbVariablesToPowerDescription
{
    /// <summary>本回合的额度是否已经用掉。</summary>
    private bool _usedThisTurn;

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override bool ShouldScaleInMultiplayer => false;

    public override string? CustomPackedIconPath => MyCharAssets.Power("adjustment");

    public override string? CustomBigIconPath => MyCharAssets.Power("adjustment");

    public override List<(string, string)>? Localization => new()
    {
        ("title", "调节"),
        ("description",
            "每回合第一次打出[gold]迭代[/gold]牌时，该牌回到手牌。\n本回合：{State}"),
    };

    /// <summary>状态栏上直接显示额度有没有用掉，省得玩家记。</summary>
    public void AddDumbVariablesToPowerDescription(LocString description) =>
        description.Add("State", _usedThisTurn ? "已使用" : "未使用");

    /// <summary>帮某个玩家试着花掉本回合的额度：拿到 true 表示这次该回手。</summary>
    public static bool TryConsumeTurnSlot(Player? player)
    {
        AdjustmentPower? power = player?.Creature?.GetPower<AdjustmentPower>();
        return power != null && power.TryConsume();
    }

    /// <summary>额度还在就花掉并返回 true；已经用过返回 false。</summary>
    private bool TryConsume()
    {
        if (_usedThisTurn)
        {
            return false;
        }
        _usedThisTurn = true;
        InvokeDisplayAmountChanged();
        return true;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner || !_usedThisTurn)
        {
            return Task.CompletedTask;
        }
        _usedThisTurn = false;
        InvokeDisplayAmountChanged();
        return Task.CompletedTask;
    }
}
