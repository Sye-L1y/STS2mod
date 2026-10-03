using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>普通运转技能：抽牌顺带补一条「抽 1 张」的传参。</summary>
[Pool(typeof(MyCharCardPool))]
public sealed class ParamGather : AlphaCard
{
    public override bool IsParamSource => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Draw", 2m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("param_gather");

    public override List<(string, string)>? Localization => new CardLoc(
        "参数采集",
        "抽{Draw:diff()}张牌。\n[gold]传参[/gold]：抽一张牌。",
        ("flavor", "采集数据，也顺手把采集本身写成一条参数。"));

    public ParamGather()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CardPileCmd.Draw(choiceContext, DynamicVars["Draw"].BaseValue, Owner);
    }

    protected override ParamEntry? BuildParam() =>
        new() { Kind = ParamKind.Draw, Value = 1m, SourceId = "PARAM_GATHER" };

    protected override void OnUpgrade()
    {
        DynamicVars["Draw"].UpgradeValueBy(1m);
    }
}