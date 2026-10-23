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
/// 稀有技能「聚神进化」（[gold]消耗[/gold]，升级后附带[gold]保留[/gold]）：使手牌中一张[gold]迭代[/gold]牌的迭代计数 +5（7）。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class ConcentratedEvolution : AlphaCard
{
    protected override IEnumerable<CardKeyword> BaseKeywords => new[]
    {
        CardKeyword.Exhaust,
    };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Boost", 5m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("concentrated_evolution");

    public override List<(string, string)>? Localization => new CardLoc(
        "聚神进化",
        "使手牌中一张[gold]迭代[/gold]牌的迭代计数+{Boost:diff()}。",
        ("flavor", "把散落的进度全部聚到一个点上。"),
        // 同「加速编译」：必须写卡面的动态变量（Boost），{Amount} 是「选几张」（=1），会显示成 +1。
        ("selectionScreenPrompt", "选择一张迭代牌，迭代计数 +{Boost:diff()}"));

    public ConcentratedEvolution()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        bool IsTarget(CardModel card) => card != this && card is AlphaCard { IterationEvery: > 0 };
        if (!PileType.Hand.GetPile(Owner).Cards.Any(IsTarget))
        {
            return;
        }

        IEnumerable<CardModel> picked = await CardSelectCmd.FromHand(choiceContext, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1), IsTarget, this);
        CardModel? card = picked.FirstOrDefault();
        if (card != null)
        {
            AlphaCorePower? core = AlphaCorePower.Of(this);
            if (core != null)
            {
                await core.AdvanceIteration(choiceContext, card, (int)DynamicVars["Boost"].BaseValue);
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Boost"].UpgradeValueBy(2m);
        // 升级后才获得「保留」，走 AddKeyword 而非 BaseKeywords，避免关键词集合缓存不刷新。
        AddKeyword(CardKeyword.Retain);
    }
}