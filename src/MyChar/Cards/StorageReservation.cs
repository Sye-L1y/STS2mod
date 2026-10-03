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
/// 普通技能「存储预留」：选择 1（2）张手牌，本回合获得[gold]保留[/gold]；
/// [gold]传参[/gold]：响应牌获得[gold]保留[/gold]。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class StorageReservation : AlphaCard
{
    public override bool IsParamSource => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Pick", 1m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("storage_reservation");

    public override List<(string, string)>? Localization => new CardLoc(
        "存储预留",
        "选择{Pick:diff()}张手牌，本回合获得[gold]保留[/gold]。\n[gold]传参[/gold]：响应牌获得[gold]保留[/gold]。",
        ("flavor", "先划一块内存出来，别让它们被刷掉。"),
        ("selectionScreenPrompt", "选择 {Amount} 张牌，本回合保留"));

    public StorageReservation()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int count = Math.Min((int)DynamicVars["Pick"].BaseValue,
            PileType.Hand.GetPile(Owner).Cards.Count(c => c != this));
        if (count <= 0)
        {
            return;
        }

        IEnumerable<CardModel> picked = await CardSelectCmd.FromHand(choiceContext, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, count), c => c != this, this);
        foreach (CardModel card in picked.ToList())
        {
            CardCmd.ApplySingleTurnRetain(card);
        }
    }

    protected override ParamEntry? BuildParam() =>
        new()
        {
            Kind = ParamKind.Keyword,
            Value = 1m,
            Variant = (int)CardKeyword.Retain,
            SourceId = "STORAGE_RESERVATION",
        };

    protected override void OnUpgrade() => DynamicVars["Pick"].UpgradeValueBy(1m);
}