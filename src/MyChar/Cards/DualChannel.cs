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
/// 罕见技能「双通道」：一条牌写两条传参——「抽 1 张」与「回复 2 点生命」。
/// 走 <see cref="AlphaCard.BuildParams"/>（一条牌多条传参）。
/// 1 费，升级减费至 0。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class DualChannel : AlphaCard
{
    public override bool IsParamSource => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Draw", 1m),
        new DynamicVar("Heal", 2m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("dual_channel");

    public override List<(string, string)>? Localization => new CardLoc(
        "双通道",
        "[gold]传参[/gold]：抽{Draw}张牌；[gold]传参[/gold]：回复{Heal}点生命。",
        ("flavor", "两条通道并行，一份输入得到两份结果。"));

    public DualChannel()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Task.CompletedTask;

    protected override IEnumerable<ParamEntry> BuildParams()
    {
        yield return new ParamEntry
        {
            Kind = ParamKind.Draw,
            Value = DynamicVars["Draw"].BaseValue,
            SourceId = "DUAL_CHANNEL",
        };
        yield return new ParamEntry
        {
            Kind = ParamKind.Heal,
            Value = DynamicVars["Heal"].BaseValue,
            SourceId = "DUAL_CHANNEL",
        };
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
