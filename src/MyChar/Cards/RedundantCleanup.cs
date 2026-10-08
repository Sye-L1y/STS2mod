using System;
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

/// <summary>
/// 罕见技能「冗余清理」：从抽牌堆选择 2（3）张牌加入手牌；
/// [gold]传参[/gold]：选择任意张手牌直接消耗（可不选、可多选）。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class RedundantCleanup : AlphaCard
{
    public override bool IsParamSource => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Pick", 2m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("cache_refresh");

    public override List<(string, string)>? Localization => new CardLoc(
        "冗余清理",
        "从抽牌堆选择{Pick:diff()}张牌加入手牌。\n[gold]传参[/gold]：选择任意张手牌消耗。",
        ("flavor", "把还能用的捡回来，把占地方的清出去。"),
        ("selectionScreenPrompt", "选择 {Amount} 张牌加入手牌"));

    public RedundantCleanup()
        : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 先按「能选到几张」收口：抽牌堆不够 / 手牌放不下时不能让选择界面要求固定张数。
        int count = Math.Min((int)DynamicVars["Pick"].BaseValue,
            CardPile.GetCards(Owner, PileType.Draw).Count());
        count = Math.Min(count, CardPile.MaxCardsInHand - PileType.Hand.GetPile(Owner).Cards.Count);
        if (count <= 0)
        {
            return;
        }

        IEnumerable<CardModel> picked = await CardSelectCmd.FromCombatPile(choiceContext,
            PileType.Draw.GetPile(Owner), Owner, new CardSelectorPrefs(SelectionScreenPrompt, count));

        foreach (CardModel card in picked.ToList())
        {
            await CardPileCmd.Add(card, PileType.Hand);
        }
    }

    protected override ParamEntry? BuildParam() =>
        new()
        {
            Kind = ParamKind.ExhaustHand,
            Value = 1m,
            SourceId = "REDUNDANT_CLEANUP",
        };

    protected override void OnUpgrade()
    {
        DynamicVars["Pick"].UpgradeValueBy(1m);
    }
}