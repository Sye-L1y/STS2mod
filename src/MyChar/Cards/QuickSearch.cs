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

/// <summary>普通运转技能：抽牌顺带推进迭代进度，抽到的迭代牌计数 +1。</summary>
[Pool(typeof(MyCharCardPool))]
public sealed class QuickSearch : AlphaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Draw", 2m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("quick_search");

    public override List<(string, string)>? Localization => new CardLoc(
        "快速检索",
        "抽{Draw:diff()}张牌。抽到的[gold]迭代[/gold]牌迭代计数+1。",
        ("flavor", "检索一遍，顺手把找到的东西往前推一格。"));

    public QuickSearch()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        IEnumerable<CardModel> drawn =
            await CardPileCmd.Draw(choiceContext, DynamicVars["Draw"].BaseValue, Owner);

        AlphaCorePower? core = AlphaCorePower.Of(this);
        if (core == null)
        {
            return;
        }

        foreach (CardModel card in drawn)
        {
            if (card is AlphaCard { IterationEvery: > 0 })
            {
                core.AddPlayCount(card, 1);
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Draw"].UpgradeValueBy(1m);
    }
}