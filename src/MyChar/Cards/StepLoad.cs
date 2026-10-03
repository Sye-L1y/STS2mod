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

/// <summary>普通迭代攻击：每打出 2 次（完成一次迭代），获得 1 点能量。</summary>
[Pool(typeof(MyCharCardPool))]
public sealed class StepLoad : AlphaCard
{
    public override int IterationEvery => 2;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new AlphaDamageVar(6m, ValueProp.Move),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("step_load");

    public override List<(string, string)>? Localization => new CardLoc(
        "逐步加载",
        "造成{Damage:diff()}点伤害。\n[gold]迭代2[/gold]（已打出{IterCount}次）：获得1点[gold]能量[/gold]。",
        ("flavor", "一次加载一段，越打越重。"));

    public StepLoad()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(RawDamage())
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override async Task OnIterationCompleted(PlayerChoiceContext choiceContext)
    {
        await PlayerCmd.GainEnergy(1m, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}