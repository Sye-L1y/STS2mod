using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace MyCharMod;

/// <summary>起始防御牌。类名 -> ModelId "card.MY_CHAR_DEFEND"。</summary>
[Pool(typeof(MyCharCardPool))]
public sealed class MyCharDefend : CustomCardModel
{
    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Defend };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(5m, ValueProp.Move),
    };

    /// <summary>卡图：assets/images/cards/defend.png（500x380）。</summary>
    public override string? CustomPortraitPath => MyCharAssets.Card("defend");

    public override List<(string, string)>? Localization => new CardLoc(
        "防御",
        "获得{Block:diff()}点[gold]格挡[/gold]。");

    public MyCharDefend()
        : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(3m);
    }
}