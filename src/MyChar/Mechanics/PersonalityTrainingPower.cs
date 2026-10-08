using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MyCharMod.Mechanics;

/// <summary>
/// 「人格训练」的载体：每次有迭代牌完成一次迭代，交替获得 1 力量 / 1 敏捷（先力量后敏捷，非随机）。
///
/// **独立实例**（<see cref="PowerInstanceType.Instanced"/>）：本牌每打出一次就多一个独立载体，
/// 各自记自己的 <see cref="Data.NextStrength"/> 交替游标——两张牌各练各的。
/// 合并会让它们共用同一个游标，交替顺序就乱了。
/// </summary>
public sealed class PersonalityTrainingPower : CustomPowerModel, IAlphaIterationListener
{
    public override PowerType Type => PowerType.Buff;

    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public override PowerStackType StackType => PowerStackType.Single;

    public override bool ShouldScaleInMultiplayer => false;

    public override string? CustomPackedIconPath => MyCharAssets.Power("personality_training");

    public override string? CustomBigIconPath => MyCharAssets.Power("personality_training");

    public override List<(string, string)>? Localization => new()
    {
        ("title", "人格训练"),
        ("description", "每次完成[gold]迭代[/gold]，交替获得1点[gold]力量[/gold]或1点[gold]敏捷[/gold]。"),
    };

    /// <summary>本载体自己的对战内状态。</summary>
    private sealed class Data
    {
        /// <summary>下一次完成迭代给力量（true）还是敏捷（false）。</summary>
        public bool NextStrength = true;
    }

    protected override object? InitInternalData() => new Data();

    private Data Core => GetInternalData<Data>();

    public async Task OnAlphaIteration(PlayerChoiceContext choiceContext, CardModel card)
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
