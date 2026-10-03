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

/// <summary>普通骨架技能：厚格挡，并把 1 点能量留到下回合（守回合也不亏节奏）。</summary>
[Pool(typeof(MyCharCardPool))]
public sealed class Firewall : AlphaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new AlphaBlockVar(14m, ValueProp.Move),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("firewall");

    public override List<(string, string)>? Localization => new CardLoc(
        "防火墙",
        "获得{Block:diff()}点[gold]格挡[/gold]。下回合获得1点[gold]能量[/gold]。",
        ("flavor", "守住这一回合，也预支下一回合。"));

    public Firewall()
        : base(2, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await PowerCmd.Apply<PendingEnergyPower>(choiceContext, Owner.Creature, 1m,
            Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(4m);
    }
}