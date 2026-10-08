using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Patches.Localization;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace MyCharMod.Mechanics;

/// <summary>
/// 「快速迭代」的载体：每张迭代牌被打出时，迭代计数额外 +1（在本牌自身记一次之后再推进一格）。
///
/// **叠加**：<see cref="PowerStackType.Counter"/>，份数记在 <see cref="PowerModel.Amount"/>，
/// 每次额外推进的格数 = 份数，状态栏数字即份数。
/// </summary>
public sealed class FastIterationPower : CustomPowerModel, IAddDumbVariablesToPowerDescription
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldScaleInMultiplayer => false;

    public override string? CustomPackedIconPath => MyCharAssets.Power("fast_iteration");

    public override string? CustomBigIconPath => MyCharAssets.Power("fast_iteration");

    public override List<(string, string)>? Localization => new()
    {
        ("title", "快速迭代"),
        ("description", "[gold]迭代[/gold]牌打出时，迭代计数额外+{Extra}。"),
    };

    /// <summary>本牌重复打出的份数（Counter 叠层层数）：先打出的那张为 1。</summary>
    private int Copies => System.Math.Max(1, Amount);

    public void AddDumbVariablesToPowerDescription(LocString description) =>
        description.Add("Extra", Copies);

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player? player = Owner.Player;
        if (player != null && cardPlay.Card is AlphaCard { IterationEvery: > 0 })
        {
            AlphaCorePower? core = AlphaCorePower.Of(player);
            if (core != null)
            {
                await core.AdvanceIteration(choiceContext, cardPlay.Card, Copies);
            }
        }
    }
}