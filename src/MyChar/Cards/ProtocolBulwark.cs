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

/// <summary>
/// 罕见技能「紧急护壁」：本回合不能打出攻击牌；获得 7（11）格挡；
/// 消耗全部协议，每层协议使格挡额外 +2（3）。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class ProtocolBulwark : AlphaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new AlphaBlockVar(7m, ValueProp.Move),
        new DynamicVar("PerLayer", 2m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("protocol_bulwark");

    public override List<(string, string)>? Localization => new CardLoc(
        "紧急护壁",
        "本回合不能打出攻击牌。获得{Block:diff()}点[gold]格挡[/gold]。\n消耗全部[gold]协议[/gold]，每层协议使格挡额外+{PerLayer:diff()}。",
        ("flavor", "把协议摊开，就是一面墙。"));

    public ProtocolBulwark()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<NoAttackPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);

        int layers = ProtocolPower.Of(this)?.ConsumeProtocol() ?? 0;
        decimal block = ResolveValue("Block") + layers * ResolveValue("PerLayer");
        await CreatureCmd.GainBlock(Owner.Creature, block, ValueProp.Move, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(4m);
        DynamicVars["PerLayer"].UpgradeValueBy(1m);
    }
}