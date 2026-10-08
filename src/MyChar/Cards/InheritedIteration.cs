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
/// 继承迭代：响应但**不消耗**传参（池留给别的响应牌用），并且每次打出永久 +1 格挡。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class InheritedIteration : AlphaCard
{
    public override bool IsResponse => true;

    public override bool ConsumesParams => false;

    public override int IterationEvery => 1;

    /// <summary>迭代成长：每打出一次 +1（升级后 +2），走 IterGain 变量以便卡面显示正确数值。</summary>
    public override IReadOnlyDictionary<string, decimal> IterationVarBonus =>
        new Dictionary<string, decimal>
        {
            ["Block"] = DynamicVars.TryGetValue("IterGain", out DynamicVar? gain) ? gain.BaseValue : 1m,
        };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new AlphaBlockVar(5m, ValueProp.Move),
        new DynamicVar("IterGain", 1m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("inherited_iteration");

    public override List<(string, string)>? Localization => new CardLoc(
        "继承迭代",
        "获得{Block:diff()}点[gold]格挡[/gold]。\n本牌不消耗[gold]传参[/gold]池。\n[gold]迭代{IterEvery}[/gold]（已打出{IterCount}次）：格挡+{IterGain:diff()}。",
        ("flavor", "每一次执行，都让下一次更厚一点。"));

    public InheritedIteration()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, RawBlock(),
            ValueProp.Move, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["IterGain"].UpgradeValueBy(1m);
    }
}