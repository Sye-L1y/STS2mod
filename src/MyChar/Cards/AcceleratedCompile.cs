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

/// <summary>罕见技能「加速编译」：使手牌中一张[gold]迭代[/gold]牌的迭代计数 +3（4）。</summary>
[Pool(typeof(MyCharCardPool))]
public sealed class AcceleratedCompile : AlphaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Boost", 3m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("accelerated_compile");

    public override List<(string, string)>? Localization => new CardLoc(
        "加速编译",
        "使手牌中一张[gold]迭代[/gold]牌的迭代计数+{Boost:diff()}。",
        ("flavor", "跳过几步，直接编译到下一段。"),
        // ★ 这里必须写卡面用的动态变量名（Boost），不能写 {Amount} —— 选择界面的 Amount 是「选几张」（=1），
        //   写它会显示成「+1」，与卡面的 +3（升级 +4）对不上。
        ("selectionScreenPrompt", "选择一张迭代牌，迭代计数 +{Boost:diff()}"));

    public AcceleratedCompile()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
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

    protected override void OnUpgrade() => DynamicVars["Boost"].UpgradeValueBy(1m);
}