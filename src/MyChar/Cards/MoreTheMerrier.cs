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

/// <summary>
/// 罕见技能「多多益善」：获得 1（2）能量；传参：获得 5（8）金币；迭代1：获得 2（3）金币。
/// 「传参」的金币入池，由响应牌消费时到账；「迭代」的金币是**本牌打出效果**，完成迭代时直接到账，不进池。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class MoreTheMerrier : AlphaCard
{
    public override int IterationEvery => 1;

    public override bool IsParamSource => true;

    /// <summary>获得型：达到档位后每次打出按层数给金币（层数 2 = 2×{IterGold} 金币）。</summary>
    public override IterationEffectKind IterationEffectKind => IterationEffectKind.Gain;

    public override IterationEffectSpec? IterationEffectSpec =>
        new("获得", DynamicVars.TryGetValue("IterGold", out DynamicVar? gold) ? gold.BaseValue : 2m, "金币");

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Energy", 1m),
        new DynamicVar("Gold", 5m),
        new DynamicVar("IterGold", 2m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("more_the_merrier");

    public override List<(string, string)>? Localization => new CardLoc(
        "多多益善",
        "获得{Energy:diff()}点[gold]能量[/gold]。\n[gold]传参[/gold]：获得{Gold:diff()}金币。\n[gold]迭代{IterEvery}[/gold]（已打出{IterCount}次）：{IterEffect}。",
        ("flavor", "越多越好，多到连金币都开始排队。"));

    public MoreTheMerrier()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayerCmd.GainEnergy(DynamicVars["Energy"].BaseValue, Owner);

        // 「获得型」迭代按层数叠加：达到档位后每次打出按层数给金币（层数不含本次，打出后计数才 +1）。
        // 初始计数 0 → 无额外效果；计数 1/2/3 → 2/4/6 金币（升级前）。
        if (IterationStacks > 0)
        {
            await PlayerCmd.GainGold(DynamicVars["IterGold"].BaseValue * IterationStacks, Owner);
        }
    }

    protected override ParamEntry? BuildParam() => new ParamEntry
    {
        Kind = ParamKind.Gold,
        Value = DynamicVars["Gold"].BaseValue,
        SourceId = "MORE_THE_MERRIER",
    };

    protected override void OnUpgrade()
    {
        DynamicVars["Energy"].UpgradeValueBy(1m);
        DynamicVars["Gold"].UpgradeValueBy(3m);
        DynamicVars["IterGold"].UpgradeValueBy(1m);
    }
}