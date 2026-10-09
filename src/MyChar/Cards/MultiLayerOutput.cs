using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 罕见攻击「多层网络输出」（X 费）：造成 7（9）点伤害 X 次；[gold]响应[/gold]；
/// 若击杀敌人，战斗结束后随机升级一张牌（由 <see cref="MultiLayerOutputPower"/> 兑现）。
///
/// X = 本次实际消耗的能量。击杀判定在 <see cref="AfterDamageGiven"/> 里看
/// <c>result.WasTargetKilled</c>（只认本牌打出的那几段伤害）。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class MultiLayerOutput : AlphaCard
{
    protected override bool HasEnergyCostX => true;

    public override bool IsResponse => true;

    /// <summary>本次结算里本牌是否打出过击杀（打完决定要不要挂升级奖励）。</summary>
    private bool _killedAny;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new AlphaDamageVar(7m, ValueProp.Move),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("multi_layer_output");

    public override List<(string, string)>? Localization => new CardLoc(
        "多层网络输出",
        "造成{Damage:diff()}点伤害X次。\n[gold]响应[/gold]。若击杀敌人，战斗结束后随机升级一张牌（每场战斗一次）。",
        ("flavor", "最后一层不再收敛，直接把结果全部推出去。"));

    public MultiLayerOutput()
        : base(0, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    public override Task AfterDamageGiven(PlayerChoiceContext choiceContext, Creature? dealer,
        DamageResult result, ValueProp props, Creature target, CardModel? cardSource)
    {
        if (cardSource == this && result.WasTargetKilled)
        {
            _killedAny = true;
        }
        return base.AfterDamageGiven(choiceContext, dealer, result, props, target, cardSource);
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        _killedAny = false;

        int times = ResolveEnergyXValue();
        for (int i = 0; i < times; i++)
        {
            // 目标已经倒下就停手：继续打只会把剩余段数浪费在同一具尸体上。
            if (cardPlay.Target.IsDead)
            {
                break;
            }
            await DamageCmd.Attack(RawDamage())
                .FromCard(this, cardPlay)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }

        if (_killedAny)
        {
            _killedAny = false;
            await PowerCmd.Apply<MultiLayerOutputPower>(choiceContext, Owner.Creature, 1m,
                Owner.Creature, this);
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
}
