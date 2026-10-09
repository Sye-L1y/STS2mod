using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 罕见技能「预输入」：下一次[gold]响应[/gold]**不消耗**[gold]传参[/gold]（池原样保留给再下一次）。
/// 1 费，升级减费（1 → 0）。额度只生效一次，由 <see cref="AlphaCorePower.SetNextResponseFree"/> 承载。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class PreInput : AlphaCard
{
    public override string? CustomPortraitPath => MyCharAssets.Card("pre_input");

    public override List<(string, string)>? Localization => new CardLoc(
        "预输入",
        "下一次[gold]响应[/gold]不消耗[gold]传参[/gold]。",
        ("flavor", "先把输入填好，等它自己读进去。"));

    public PreInput()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        AlphaCorePower.Of(this)?.SetNextResponseFree();
        return Task.CompletedTask;
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
