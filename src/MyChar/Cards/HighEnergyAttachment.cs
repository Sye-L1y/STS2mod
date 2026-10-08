using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>罕见技能「高能附加」：抽 3（4）张牌；抽到的[gold]迭代[/gold]牌本回合费用 -1。</summary>
[Pool(typeof(MyCharCardPool))]
public sealed class HighEnergyAttachment : AlphaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Draw", 3m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("high_energy_attachment");

    public override List<(string, string)>? Localization => new CardLoc(
        "高能附加",
        "抽{Draw:diff()}张牌。抽到的[gold]迭代[/gold]牌本回合费用-1。",
        ("flavor", "给这些循环临时接上一条高能支路。"));

    public HighEnergyAttachment()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        IEnumerable<CardModel> drawn =
            await CardPileCmd.Draw(choiceContext, DynamicVars["Draw"].BaseValue, Owner);

        foreach (CardModel card in drawn)
        {
            if (card is AlphaCard { IterationEvery: > 0 })
            {
                card.EnergyCost.AddThisTurn(-1, reduceOnly: true);
            }
        }
    }

    protected override void OnUpgrade() => DynamicVars["Draw"].UpgradeValueBy(1m);
}