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
        if (times > 0)
        {
            // 与本体「旋风斩」等 X 段攻击同口径：**一次** Attack 命令打 X 段（WithHitCount），
            // 而不是循环 X 次独立 Attack。分开循环时每一段都算一次独立攻击（各自触发 BeforeAttack /
            // AfterAttack），「下一次攻击」类加成（活力）只会落在第一段；单命令则整次算一次攻击，
            // 每一段都吃满加成，并尊重 Hook.ModifyAttackHitCount 的命中次数修正。
            // 目标中途倒下时，命令内部的循环会自动 break，剩余段数不会浪费。
            await DamageCmd.Attack(RawDamage())
                .FromCard(this, cardPlay)
                .Targeting(cardPlay.Target)
                .WithHitCount(times)
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
