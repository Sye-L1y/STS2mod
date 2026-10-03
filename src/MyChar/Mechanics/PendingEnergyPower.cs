using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Patches.Localization;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;

namespace MyCharMod.Mechanics;

/// <summary>
/// 「下回合获得 N 点能量」的延迟载体（防火墙）。
///
/// 用 <c>AfterPlayerTurnStart</c>：本回合打出时回合开始已经过去，所以会在**下一个**己方回合
/// 开始时发放能量。层数走 Amount，同类效果叠加。
/// </summary>
public sealed class PendingEnergyPower : CustomPowerModel, IAddDumbVariablesToPowerDescription
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldScaleInMultiplayer => false;

    public override string? CustomPackedIconPath => MyCharAssets.Power("pending_energy");

    public override string? CustomBigIconPath => MyCharAssets.Power("pending_energy");

    public override List<(string, string)>? Localization => new()
    {
        ("title", "待发能量"),
        ("description", "下回合开始时，获得{Energy}点[gold]能量[/gold]。"),
    };

    public void AddDumbVariablesToPowerDescription(LocString description) =>
        description.Add("Energy", Amount);

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner)
        {
            return;
        }

        await PlayerCmd.GainEnergy(Amount, player);
        await PowerCmd.Remove(this);
    }
}