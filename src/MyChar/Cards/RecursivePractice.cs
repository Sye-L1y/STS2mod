using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>普通迭代技能：每打出 2 次，格挡永久 +2（本场战斗）。</summary>
[Pool(typeof(MyCharCardPool))]
public sealed class RecursivePractice : AlphaCard
{
    public override int IterationEvery => 2;

    public override IReadOnlyDictionary<string, decimal> IterationVarBonus =>
        new Dictionary<string, decimal> { ["Block"] = 2m };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new AlphaBlockVar(4m, ValueProp.Move),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("recursive_practice");

    public override List<(string, string)>? Localization => new CardLoc(
        "递归练习",
        "获得{Block:diff()}点[gold]格挡[/gold]。\n[gold]迭代2[/gold]（已打出{IterCount}次）：格挡+2。",
        ("flavor", "同样的动作，重复出更厚的甲。"));

    public RecursivePractice()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, RawBlock(),
            ValueProp.Move, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
    }
}