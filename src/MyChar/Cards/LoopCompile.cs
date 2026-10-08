using System.Collections.Generic;
using System.Linq;
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

/// <summary>罕见运转技能：消耗；把弃牌堆里的迭代牌整批调回抽牌堆顶，然后抽 2（3）张。</summary>
[Pool(typeof(MyCharCardPool))]
public sealed class LoopCompile : AlphaCard
{
    protected override IEnumerable<CardKeyword> BaseKeywords => new[] { CardKeyword.Exhaust };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Draw", 2m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("loop_compile");

    public override List<(string, string)>? Localization => new CardLoc(
        "循环编译",
        "将弃牌堆中所有[gold]迭代[/gold]牌置于抽牌堆顶，然后抽{Draw:diff()}张牌。",
        ("flavor", "把没跑完的循环重新排到最前面。"));

    public LoopCompile()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        List<CardModel> iterationCards = CardPile.GetCards(Owner, PileType.Discard)
            .Where(card => card is AlphaCard { IterationEvery: > 0 })
            .ToList();

        if (iterationCards.Count > 0)
        {
            await CardPileCmd.Add(iterationCards, PileType.Draw, CardPilePosition.Top);
        }

        await CardPileCmd.Draw(choiceContext, DynamicVars["Draw"].BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Draw"].UpgradeValueBy(1m);
    }
}