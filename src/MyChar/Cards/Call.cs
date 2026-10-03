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

/// <summary>罕见运转技能：从抽牌堆里按机制筛牌直接上手，带消耗防止无限循环。</summary>
[Pool(typeof(MyCharCardPool))]
public sealed class Call : AlphaCard
{
    protected override IEnumerable<CardKeyword> BaseKeywords => new[] { CardKeyword.Exhaust };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Pick", 2m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("call");

    public override List<(string, string)>? Localization => new CardLoc(
        "调用",
        "从抽牌堆选择{Pick:diff()}张带[gold]传参[/gold]、[gold]响应[/gold]、[gold]迭代[/gold]或[gold]协议[/gold]的牌，加入手牌。",
        ("flavor", "按名字把需要的那段代码叫回来。"),
        ("selectionScreenPrompt", "选择 {Amount} 张牌加入手牌"));

    public Call()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 先按「能选到几张」收口：抽牌堆里联动牌不够时不能让选择界面要求固定张数。
        int count = Math.Min((int)DynamicVars["Pick"].BaseValue,
            CardPile.GetCards(Owner, PileType.Draw).Count(IsLinked));
        count = Math.Min(count, CardPile.MaxCardsInHand - PileType.Hand.GetPile(Owner).Cards.Count);
        if (count <= 0)
        {
            return;
        }

        IEnumerable<CardModel> picked = await CardSelectCmd.FromCombatPile(choiceContext,
            PileType.Draw.GetPile(Owner), Owner, new CardSelectorPrefs(SelectionScreenPrompt, count),
            IsLinked);

        foreach (CardModel card in picked.ToList())
        {
            await CardPileCmd.Add(card, PileType.Hand);
        }
    }

    /// <summary>带任一核心机制的牌 = 值得调用。</summary>
    private static bool IsLinked(CardModel card) =>
        card is AlphaCard alpha &&
        (alpha.IsParamSource || alpha.IsResponse || alpha.IterationEvery > 0 || alpha.GrantsProtocol);

    protected override void OnUpgrade()
    {
        DynamicVars["Pick"].UpgradeValueBy(1m);
    }
}