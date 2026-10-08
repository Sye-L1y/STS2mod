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
using MegaCrit.Sts2.Core.Models.Powers;

namespace MyCharMod.Mechanics;

/// <summary>
/// 「人格训练」的载体：每次有迭代牌完成一次迭代，交替获得 1 力量 / 1 敏捷（先力量后敏捷，非随机）。
///
/// **叠加**：<see cref="PowerStackType.Counter"/>，份数记在 <see cref="PowerModel.Amount"/>，
/// 每次迭代按份数连续结算（2 份即 1 力量 + 1 敏捷），状态栏数字即份数。
/// </summary>
public sealed class PersonalityTrainingPower : CustomPowerModel, IAlphaIterationListener,
    IAddDumbVariablesToPowerDescription
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldScaleInMultiplayer => false;

    public override string? CustomPackedIconPath => MyCharAssets.Power("personality_training");

    public override string? CustomBigIconPath => MyCharAssets.Power("personality_training");

    public override List<(string, string)>? Localization => new()
    {
        ("title", "人格训练"),
        ("description", "每次完成[gold]迭代[/gold]，交替获得{Points}点[gold]力量[/gold]/[gold]敏捷[/gold]。"),
    };

    private sealed class Data
    {
        /// <summary>下一次完成迭代给力量（true）还是敏捷（false）。</summary>
        public bool NextStrength = true;
    }

    protected override object? InitInternalData() => new Data();

    private Data Core => GetInternalData<Data>();

    /// <summary>本牌重复打出的份数（Counter 叠层层数）：先打出的那张为 1。</summary>
    private int Copies => System.Math.Max(1, Amount);

    public void AddDumbVariablesToPowerDescription(LocString description) =>
        description.Add("Points", Copies);

    public async Task OnAlphaIteration(PlayerChoiceContext choiceContext, CardModel card)
    {
        for (int i = 0; i < Copies; i++)
        {
            if (Core.NextStrength)
            {
                await PowerCmd.Apply<StrengthPower>(choiceContext, Owner, 1m, Owner, null);
            }
            else
            {
                await PowerCmd.Apply<DexterityPower>(choiceContext, Owner, 1m, Owner, null);
            }
            Core.NextStrength = !Core.NextStrength;
        }
    }
}
