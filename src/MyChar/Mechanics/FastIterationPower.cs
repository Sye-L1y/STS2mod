using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace MyCharMod.Mechanics;

/// <summary>
/// 「快速迭代」的载体：每张迭代牌被打出时，迭代计数额外 +1（在本牌自身记一次之后再推进一格）。
/// </summary>
public sealed class FastIterationPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override bool ShouldScaleInMultiplayer => false;

    public override string? CustomPackedIconPath => MyCharAssets.Power("fast_iteration");

    public override string? CustomBigIconPath => MyCharAssets.Power("fast_iteration");

    public override List<(string, string)>? Localization => new()
    {
        ("title", "快速迭代"),
        ("description", "[gold]迭代[/gold]牌打出时，迭代计数额外+1。"),
    };

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        Player? player = Owner.Player;
        if (player != null && cardPlay.Card is AlphaCard { IterationEvery: > 0 })
        {
            AlphaCorePower? core = AlphaCorePower.Of(player);
            if (core != null)
            {
                await core.AdvanceIteration(choiceContext, cardPlay.Card, 1);
            }
        }
    }
}