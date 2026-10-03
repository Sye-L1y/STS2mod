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
/// 罕见技能「无限创想」：将一张随机无色牌加入手牌；
/// [gold]传参[/gold]：1（2）张随机无色牌加入手牌。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class InfiniteImagination : AlphaCard
{
    public override bool IsParamSource => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Param", 1m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("infinite_imagination");

    public override List<(string, string)>? Localization => new CardLoc(
        "无限创想",
        "将一张随机[gold]无色牌[/gold]加入手牌。\n[gold]传参[/gold]：{Param:diff()}张随机[gold]无色牌[/gold]加入手牌。",
        ("flavor", "没有的牌，就现想一张出来。"),
        ("selectionScreenPrompt", "选择 {Amount} 张牌加入手牌"));

    public InfiniteImagination()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await GenerateColorless(choiceContext, 1);
    }

    protected override ParamEntry? BuildParam() =>
        new()
        {
            Kind = ParamKind.GenerateCard,
            Value = DynamicVars["Param"].BaseValue,
            Variant = (int)GenCardKind.ColorlessRandom,
            SourceId = "INFINITE_IMAGINATION",
        };

    protected override void OnUpgrade() => DynamicVars["Param"].UpgradeValueBy(1m);
}