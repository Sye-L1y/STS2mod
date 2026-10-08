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
/// 普通迭代攻击：每打出 2 次（完成一次迭代），本牌永久获得「每次打出 +1 能量」。
/// 「获得型」迭代按层数叠加：达到档位后，每次打出按当前层数给能量（层数不含本次，打出后计数才 +1）。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class StepLoad : AlphaCard
{
    public override int IterationEvery => 2;

    /// <summary>获得型：达到档位后，每次打出按当前层数给能量（层数 2 = 2 点）。</summary>
    public override IterationEffectKind IterationEffectKind => IterationEffectKind.Gain;

    public override IterationEffectSpec? IterationEffectSpec => new("打出后获得", 1m, "点[gold]能量[/gold]");

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new AlphaDamageVar(6m, ValueProp.Move),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("step_load");

    public override List<(string, string)>? Localization => new CardLoc(
        "逐步加载",
        "造成{Damage:diff()}点伤害。\n[gold]迭代[/gold]2（已打出{IterCount}次）：{IterEffect}。",
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

        // 达到档位后每次打出按层数给能量；计数在本次打出后 +1，所以这里用打出前的层数。
        if (IterationStacks > 0)
        {
            await PlayerCmd.GainEnergy(IterationStacks, Owner);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}