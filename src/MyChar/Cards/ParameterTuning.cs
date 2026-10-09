using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 稀有攻击「调参」：对所有敌人造成 6 点伤害，当前每有一条[gold]传参[/gold]，伤害 +5（7）。
/// 不消费传参，只读取池中条数。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class ParameterTuning : AlphaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new AlphaDamageVar(6m, ValueProp.Move),
        new DynamicVar("PerParam", 5m),
    };

    /// <summary>
    /// 「每有一条传参伤害 +N」当作伤害成长并入 <see cref="AlphaCard.RawDamage"/> 的口径：
    /// 预览（<see cref="AlphaDamageVar"/>）与实战（<c>RawDamage()</c>）都走这里，卡面数字与实打一致。
    /// 本牌不迭代（<see cref="AlphaCard.IterationEvery"/> = 0），这里复用的是「基础值之外的成长」这一通道。
    /// </summary>
    protected override decimal IterationBonusFor(string varName)
    {
        if (varName == "Damage")
        {
            int count = AlphaCorePower.Of(this)?.ParamCount ?? 0;
            decimal per = DynamicVars.TryGetValue("PerParam", out DynamicVar? value) ? value.BaseValue : 5m;
            return count * per;
        }
        return base.IterationBonusFor(varName);
    }

    public override string? CustomPortraitPath => MyCharAssets.Card("parameter_tuning");

    public override List<(string, string)>? Localization => new CardLoc(
        "调参",
        "对所有敌人造成{Damage:diff()}点伤害。当前每有一条[gold]传参[/gold]，伤害+{PerParam:diff()}。",
        ("flavor", "参数越多，这一刀越沉。"));

    public ParameterTuning()
        : base(1, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ICombatState? combatState = CombatState ?? Owner.Creature.CombatState;
        if (combatState == null)
        {
            return;
        }
        await DamageCmd.Attack(RawDamage())
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(combatState)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars["PerParam"].UpgradeValueBy(2m);
}