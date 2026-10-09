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
/// 罕见能力「惯性」：每有一张[gold]迭代[/gold]牌被打出，就抽 1 张牌。
/// 由 <see cref="InertiaPower"/> 承载。1 费，升级减费至 0。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class Inertia : AlphaCard
{
    public override string? CustomPortraitPath => MyCharAssets.Card("inertia");

    public override List<(string, string)>? Localization => new CardLoc(
        "惯性",
        "[gold]迭代[/gold]牌打出后，抽1张牌。",
        ("flavor", "一旦动起来，就停不下来了。"));

    public Inertia()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<InertiaPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
