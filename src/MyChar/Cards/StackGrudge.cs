using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 罕见攻击「叠仇」：消耗；造成 10 伤害，额外造成等同于自身已损失生命 50%（100%）的伤害；
/// 目标身上每有一种**不同的**负面效果，这份额外伤害的倍率 +20%（25%）。
///
/// 「负面效果」按目标敌人身上 PowerType.Debuff 的条目统计，并按 Power 类型去重
/// （每种状态只算一次）——和本职业「负面状态作用于本次目标」的口径一致。
/// 额外伤害与基础伤害合并为一次攻击结算，力量 / 活力照常作用其上。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class StackGrudge : AlphaCard
{
    protected override IEnumerable<CardKeyword> BaseKeywords => new[] { CardKeyword.Exhaust };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new AlphaDamageVar(10m, ValueProp.Move),
        new DynamicVar("LostHpPct", 50m),
        new DynamicVar("PerDebuff", 20m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("stack_grudge");

    public override List<(string, string)>? Localization => new CardLoc(
        "叠仇",
        "造成{Damage:diff()}点伤害，额外造成等同于自身已损失生命{LostHpPct:diff()}%的伤害。\n目标身上每有一种不同的[gold]负面效果[/gold]，这份额外伤害的倍率+{PerDebuff:diff()}%。",
        ("flavor", "它记着每一次受伤，也记着每一个被削弱的敌人。"));

    public StackGrudge()
        : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        Creature self = Owner.Creature;
        decimal lostHp = Math.Max(self.MaxHp - self.CurrentHp, 0);
        decimal ratio = DynamicVars["LostHpPct"].BaseValue / 100m;
        decimal multiplier = 1m + DistinctDebuffCount(cardPlay.Target)
            * DynamicVars["PerDebuff"].BaseValue / 100m;
        decimal bonus = Math.Floor(lostHp * ratio * multiplier);

        await DamageCmd.Attack(RawDamage() + bonus)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    /// <summary>目标身上不同种类负面效果的数量（按 Power 类型去重）。</summary>
    private static int DistinctDebuffCount(Creature target) =>
        target.Powers
            .Where(power => power.TypeForCurrentAmount == PowerType.Debuff)
            .Select(power => power.GetType())
            .Distinct()
            .Count();

    protected override void OnUpgrade()
    {
        DynamicVars["LostHpPct"].UpgradeValueBy(50m);
        DynamicVars["PerDebuff"].UpgradeValueBy(5m);
    }
}