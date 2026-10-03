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

/// <summary>罕见技能：高费厚格挡，并给下一条传参「响应牌伤害 +6」。</summary>
[Pool(typeof(MyCharCardPool))]
public sealed class OverclockProtocol : AlphaCard
{
    public override bool IsParamSource => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new AlphaBlockVar(12m, ValueProp.Move),
        new DynamicVar("Add", 6m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("overclock_protocol");

    public override List<(string, string)>? Localization => new CardLoc(
        "超频协议",
        "获得{Block:diff()}点[gold]格挡[/gold]。\n[gold]传参[/gold]：响应牌伤害+{Add:diff()}。",
        ("flavor", "超频的代价是，下一次出手会更重。"));

    public OverclockProtocol()
        : base(2, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, RawBlock(), ValueProp.Move, cardPlay);
    }

    protected override ParamEntry? BuildParam() =>
        new()
        {
            Kind = ParamKind.DamageAdd,
            Value = DynamicVars["Add"].BaseValue,
            SourceId = "OVERCLOCK_PROTOCOL",
        };

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(4m);
        DynamicVars["Add"].UpgradeValueBy(3m);
    }
}