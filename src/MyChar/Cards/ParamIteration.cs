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

/// <summary>普通响应迭代攻击：清池的同时，每打出 2 次伤害永久 +2。</summary>
[Pool(typeof(MyCharCardPool))]
public sealed class ParamIteration : AlphaCard
{
    public override bool IsResponse => true;

    public override int IterationEvery => 2;

    public override IReadOnlyDictionary<string, decimal> IterationVarBonus =>
        new Dictionary<string, decimal> { ["Damage"] = 2m };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new AlphaDamageVar(6m, ValueProp.Move),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("param_iteration");

    public override List<(string, string)>? Localization => new CardLoc(
        "传参迭代",
        "造成{Damage:diff()}点伤害。\n[gold]迭代2[/gold]（已打出{IterCount}次）：伤害+2。",
        ("flavor", "把每一次应答都编译进下一次。"));

    public ParamIteration()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
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

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}