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

/// <summary>罕见运转技能：比「快速检索」推得更狠，抽到的迭代牌一次推进两格。</summary>
[Pool(typeof(MyCharCardPool))]
public sealed class WarmupLoop : AlphaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Draw", 2m),
        new DynamicVar("Boost", 1m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("warmup_loop");

    public override List<(string, string)>? Localization => new CardLoc(
        "预热循环",
        "抽{Draw:diff()}张牌。抽到的[gold]迭代[/gold]牌迭代计数+{Boost:diff()}。",
        ("flavor", "先把循环跑热，再正式开工。"));

    public WarmupLoop()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
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

        int boost = (int)DynamicVars["Boost"].BaseValue;
        foreach (CardModel card in drawn)
        {
            if (card is AlphaCard { IterationEvery: > 0 })
            {
                core.AddPlayCount(card, boost);
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Boost"].UpgradeValueBy(1m);
    }
}