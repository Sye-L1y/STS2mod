using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Patches.Localization;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;

namespace MyCharMod.Mechanics;

/// <summary>
/// 「高速升级」的载体：接下来打出的 <c>Amount</c> 张[gold]迭代[/gold]牌，迭代计数额外 +2。
/// 每打出一张迭代牌消耗一次；次数用尽即移除，**不**受回合结束限制（跨回合保留）。
/// </summary>
public sealed class HighSpeedUpgradePower : CustomPowerModel, IAddDumbVariablesToPowerDescription
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldScaleInMultiplayer => false;

    public override string? CustomPackedIconPath => MyCharAssets.Power("fast_iteration");

    public override string? CustomBigIconPath => MyCharAssets.Power("fast_iteration");

    public override List<(string, string)>? Localization => new()
    {
        ("title", "高速升级"),
        ("description", "接下来打出的{Amount}张[gold]迭代[/gold]牌，迭代计数额外+2。"),
    };

    public void AddDumbVariablesToPowerDescription(LocString description) =>
        description.Add("Amount", Amount);

    public override async Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Amount <= 0 || cardPlay.Card is not AlphaCard { IterationEvery: > 0 })
        {
            return;
        }

        Player? player = Owner.Player;
        AlphaCorePower? core = player == null ? null : AlphaCorePower.Of(player);
        if (core == null)
        {
            return;
        }

        await core.AdvanceIteration(choiceContext, cardPlay.Card, 2);
        await PowerCmd.ModifyAmount(choiceContext, this, -1m, null, null);
    }
}