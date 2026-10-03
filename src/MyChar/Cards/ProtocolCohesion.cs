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

/// <summary>罕见技能：格挡 + 下回合协议，守回合的同时为高档位铺路。</summary>
[Pool(typeof(MyCharCardPool))]
public sealed class ProtocolCohesion : AlphaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new AlphaBlockVar(6m, ValueProp.Move),
        new DynamicVar("Protocol", 3m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("protocol_cohesion");

    public override List<(string, string)>? Localization => new CardLoc(
        "协议凝聚",
        "获得{Block:diff()}点[gold]格挡[/gold]。下回合获得{Protocol:diff()}层[gold]协议[/gold]。",
        ("flavor", "把散落的权限凝成一条，留到下一回合。"));

    public ProtocolCohesion()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
        await PowerCmd.Apply<PendingProtocolPower>(choiceContext, Owner.Creature,
            DynamicVars["Protocol"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
        DynamicVars["Protocol"].UpgradeValueBy(1m);
    }
}