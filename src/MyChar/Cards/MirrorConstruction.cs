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
/// 稀有技能「镜像构建」（[gold]消耗[/gold]）：本牌无即时效果；
/// [gold]传参[/gold]：将响应牌的 1（2）张复制放入手牌。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class MirrorConstruction : AlphaCard
{
    public override bool IsParamSource => true;

    protected override IEnumerable<CardKeyword> BaseKeywords => new[] { CardKeyword.Exhaust };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Copy", 1m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("mirror_construction");

    public override List<(string, string)>? Localization => new CardLoc(
        "镜像构建",
        "[gold]传参[/gold]：将响应牌的{Copy:diff()}张复制放入手牌。",
        ("flavor", "把此刻的应答复制一份，留给下一次。"));

    public MirrorConstruction()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Task.CompletedTask;

    protected override ParamEntry? BuildParam() =>
        new()
        {
            Kind = ParamKind.GenerateCard,
            Value = DynamicVars["Copy"].BaseValue,
            Variant = (int)GenCardKind.CopyThisCard,
            SourceId = "MIRROR_CONSTRUCTION",
        };

    protected override void OnUpgrade() => DynamicVars["Copy"].UpgradeValueBy(1m);
}