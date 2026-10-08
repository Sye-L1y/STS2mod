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
/// 稀有能力「快速迭代」：本场战斗中，每张[gold]迭代[/gold]牌被打出时，迭代计数额外 +1。
/// 升级只减费（3 → 2）。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class FastIteration : AlphaCard
{
    public override string? CustomPortraitPath => MyCharAssets.Card("fast_iteration");

    public override List<(string, string)>? Localization => new CardLoc(
        "快速迭代",
        "[gold]迭代[/gold]牌打出时，迭代计数额外+1。",
        ("flavor", "把循环跑快一点，进度自然就上来了。"));

    public FastIteration()
        : base(3, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<FastIterationPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}