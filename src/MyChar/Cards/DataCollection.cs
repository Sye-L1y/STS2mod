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
using MegaCrit.Sts2.Core.ValueProps;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>罕见运转攻击：打完顺手把弃牌堆里想要的牌捞回抽牌堆顶，下回合就能摸到。</summary>
[Pool(typeof(MyCharCardPool))]
public sealed class DataCollection : AlphaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new AlphaDamageVar(8m, ValueProp.Move),
        new DynamicVar("Pick", 1m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("data_collection");

    public override List<(string, string)>? Localization => new CardLoc(
        "数据收集",
        "造成{Damage:diff()}点伤害。从弃牌堆选择{Pick:diff()}张牌，置于抽牌堆顶。",
        ("flavor", "把丢掉的数据捡回来，下次就能先用上。"),
        ("selectionScreenPrompt", "选择 {Amount} 张牌置于抽牌堆顶"));

    public DataCollection()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(RawDamage())
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        int count = Math.Min((int)DynamicVars["Pick"].BaseValue,
            PileType.Discard.GetPile(Owner).Cards.Count);
        if (count <= 0)
        {
            return;
        }

        IEnumerable<CardModel> picked = await CardSelectCmd.FromCombatPile(choiceContext,
            PileType.Discard.GetPile(Owner), Owner, new CardSelectorPrefs(SelectionScreenPrompt, count));

        foreach (CardModel card in picked.ToList())
        {
            await CardPileCmd.Add(card, PileType.Draw, CardPilePosition.Top);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Pick"].UpgradeValueBy(1m);
    }
}