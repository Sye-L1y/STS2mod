using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 稀有能力和「梯度回归」：每次触发[gold]响应[/gold]时，获得「本次响应消耗的传参数 ×3」点[gold]格挡[/gold]。
/// 升级只减费（2 → 1）。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class GradientRegression : AlphaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Factor", 3m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("gradient_regression");

    public override List<(string, string)>? Localization => new CardLoc(
        "梯度回归",
        "每次触发[gold]响应[/gold]时，获得「本次响应消耗的传参数×{Factor:diff()}」点[gold]格挡[/gold]。",
        ("flavor", "响应留下的梯度，全部折算成护甲。"));

    public GradientRegression()
        : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<GradientRegressionPower>(choiceContext, Owner.Creature,
            DynamicVars["Factor"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}