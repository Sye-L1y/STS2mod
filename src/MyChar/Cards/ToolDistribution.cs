using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 普通技能「小刀塑形」：打出直接获得 2（3）张随机合法附魔的小刀；
/// 另外留一条「加入附魔小刀」的传参，由下一张响应牌消费时再进 1（2）张。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class ToolDistribution : AlphaCard
{
    public override bool IsParamSource => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Count", 2m),
        new DynamicVar("Param", 1m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("tool_distribution");

    public override List<(string, string)>? Localization => new CardLoc(
        "小刀塑形",
        "获得{Count:diff()}张随机合法附魔的小刀。\n[gold]传参[/gold]：{Param:diff()}张随机合法附魔的小刀加入手牌。",
        ("flavor", "库存直接塞进手里，比什么调度都快。"));

    public ToolDistribution()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await GenerateEnchantedShivs(choiceContext, (int)DynamicVars["Count"].BaseValue);
    }

    protected override ParamEntry? BuildParam() =>
        new()
        {
            Kind = ParamKind.GenerateCard,
            Value = DynamicVars["Param"].BaseValue,
            Variant = (int)GenCardKind.EnchantedShiv,
            SourceId = "TOOL_DISTRIBUTION",
        };

    protected override void OnUpgrade()
    {
        DynamicVars["Count"].UpgradeValueBy(1m);
        DynamicVars["Param"].UpgradeValueBy(1m);
    }
}