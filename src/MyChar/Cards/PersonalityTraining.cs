using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 稀有能力「人格训练」：每次完成[gold]迭代[/gold]，交替获得 1 [gold]力量[/gold]或 1 [gold]敏捷[/gold]（先力量后敏捷，非随机）。
/// 升级只减费（3 → 2）。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class PersonalityTraining : AlphaCard
{
    public override string? CustomPortraitPath => MyCharAssets.Card("personality_training");

    public override List<(string, string)>? Localization => new CardLoc(
        "人格训练",
        "每次完成[gold]迭代[/gold]，交替获得1点[gold]力量[/gold]或1点[gold]敏捷[/gold]。",
        ("flavor", "一遍练力量，一遍练敏捷，谁都不落下。"));

    public PersonalityTraining()
        : base(3, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<PersonalityTrainingPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}