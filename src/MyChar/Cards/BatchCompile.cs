using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 稀有技能「批量编译」（X 费，[gold]消耗[/gold]）：使手牌中所有[gold]迭代[/gold]牌计数 +X（升级后 +X+1）。
/// X = 本次实际消耗的能量。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class BatchCompile : AlphaCard
{
    protected override bool HasEnergyCostX => true;

    protected override IEnumerable<CardKeyword> BaseKeywords => new[] { CardKeyword.Exhaust };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Bonus", 0m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("batch_compile");

    public override List<(string, string)>? Localization => new CardLoc(
        "批量编译",
        "使手牌中所有[gold]迭代[/gold]牌计数+{IfUpgraded:show:X+1|X}。",
        ("flavor", "一次性把所有循环都往前推一格。"));

    public BatchCompile()
        : base(0, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int amount = ResolveEnergyXValue() + (int)DynamicVars["Bonus"].BaseValue;
        AlphaCorePower? core = AlphaCorePower.Of(this);
        if (core == null || amount <= 0)
        {
            return;
        }

        foreach (CardModel card in PileType.Hand.GetPile(Owner).Cards)
        {
            if (card is AlphaCard { IterationEvery: > 0 })
            {
                await core.AdvanceIteration(choiceContext, card, amount);
            }
        }
    }

    protected override void OnUpgrade() => DynamicVars["Bonus"].UpgradeValueBy(1m);
}