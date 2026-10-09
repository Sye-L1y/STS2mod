using System;
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
/// 罕见攻击「数据模拟」：造成 12（18）伤；[gold]迭代2[/gold]：伤害**翻倍**。
/// 层数 &gt; 0（即已打出满 2 次）后每次打出伤害 ×2（乘在本次增幅之后的原始值上）。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class DataSimulation : AlphaCard
{
    public override int IterationEvery => 2;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new AlphaDamageVar(12m, ValueProp.Move),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("data_simulation");

    public override List<(string, string)>? Localization => new CardLoc(
        "数据模拟",
        "造成{Damage:diff()}点伤害。\n[gold]迭代2[/gold]（已打出{IterCount}次）：伤害翻倍。",
        ("flavor", "跑一遍模拟，把结果当成真的打出去。"));

    public DataSimulation()
        : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        decimal damage = RawDamage();
        if (IterationStacks > 0)
        {
            damage *= 2m;
        }
        await DamageCmd.Attack(damage)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(6m);
}
