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

/// <summary>罕见技能：厚格挡 + 给下一条传参「响应牌伤害翻倍」。3 费，升级减费至 2。</summary>
[Pool(typeof(MyCharCardPool))]
public sealed class CondenseQi : AlphaCard
{
    public override bool IsParamSource => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new AlphaBlockVar(18m, ValueProp.Move),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("condense_qi");

    public override List<(string, string)>? Localization => new CardLoc(
        "凝神聚气",
        "获得{Block:diff()}点[gold]格挡[/gold]。\n[gold]传参[/gold]：响应牌伤害增加一倍。",
        ("flavor", "把气聚到一处，下一次爆发会更重。"));

    public CondenseQi()
        : base(3, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, cardPlay);
    }

    protected override ParamEntry? BuildParam() =>
        new() { Kind = ParamKind.DamageBoost, Value = 1m, SourceId = "CONDENSE_QI" };

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}