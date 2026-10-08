using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Patches.Localization;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace MyCharMod.Mechanics;

/// <summary>
/// 「梯度回归」的载体：每次触发[gold]响应[/gold]时，按**本次响应消耗的传参条数** × Amount 获得格挡。
/// 消耗条数从 <see cref="AlphaCorePower.LastConsumed"/> 读取。
/// </summary>
public sealed class GradientRegressionPower : CustomPowerModel, IAlphaResponseListener,
    IAddDumbVariablesToPowerDescription
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldScaleInMultiplayer => false;

    public override string? CustomPackedIconPath => MyCharAssets.Power("param_iteration");

    public override string? CustomBigIconPath => MyCharAssets.Power("param_iteration");

    public override List<(string, string)>? Localization => new()
    {
        ("title", "梯度回归"),
        ("description", "每次触发[gold]响应[/gold]时，获得「本次响应消耗的传参数×{Factor}」点[gold]格挡[/gold]。"),
    };

    public void AddDumbVariablesToPowerDescription(LocString description) =>
        description.Add("Factor", Amount);

    public async Task OnAlphaResponse(PlayerChoiceContext choiceContext, CardModel card)
    {
        AlphaCorePower? core = Owner.Player == null ? null : AlphaCorePower.Of(Owner.Player);
        int count = core?.LastConsumed.Count ?? 0;
        if (count <= 0)
        {
            return;
        }
        await CreatureCmd.GainBlock(Owner, count * Amount, ValueProp.Move, null);
    }
}