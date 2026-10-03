using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>普通运转技能：把手里用不上的换掉，0 费，纯换牌。</summary>
[Pool(typeof(MyCharCardPool))]
public sealed class CacheRefresh : AlphaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Draw", 1m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("cache_refresh");

    public override List<(string, string)>? Localization => new CardLoc(
        "缓存刷新",
        "弃1张手牌，抽{Draw:diff()}张牌。",
        ("flavor", "清掉缓存，重新读一遍。"));

    public CacheRefresh()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        List<CardModel> picked = (await CardSelectCmd.FromHandForDiscard(choiceContext, Owner,
            new CardSelectorPrefs(CardSelectorPrefs.DiscardSelectionPrompt, 1), null, this)).ToList();

        if (picked.Count > 0)
        {
            await CardCmd.Discard(choiceContext, picked);
        }

        await CardPileCmd.Draw(choiceContext, DynamicVars["Draw"].BaseValue, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Draw"].UpgradeValueBy(1m);
    }
}