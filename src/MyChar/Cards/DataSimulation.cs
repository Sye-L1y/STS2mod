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
///
/// 「翻倍」按层数**每层翻一倍**：层数 n（每打出 2 次叠 1 层）→ 伤害 ×2ⁿ，即 1/2/3 层 = ×2/×4/×8
/// （设计上就是指数增长，见 docs/07 评估表）。
/// 乘数走 <see cref="AlphaCard.DamageMultiplier"/>，实战与卡面预览同一条口径 → 卡面 {Damage} 显示的就是实打值。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class DataSimulation : AlphaCard
{
    public override int IterationEvery => 2;

    /// <summary>迭代乘法：层数 n → ×2ⁿ（0 层 = ×1）。实战与卡面共用。</summary>
    protected override decimal DamageMultiplier
    {
        get
        {
            decimal multiplier = 1m;
            for (int i = 0; i < IterationStacks; i++)
            {
                multiplier *= 2m;
            }
            return multiplier;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new AlphaDamageVar(12m, ValueProp.Move),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("data_simulation");

    public override List<(string, string)>? Localization => new CardLoc(
        "数据模拟",
        "造成{Damage:diff()}点伤害。\n[gold]迭代2[/gold]：伤害翻倍。",
        ("flavor", "跑一遍模拟，把结果当成真的打出去。"));

    public DataSimulation()
        : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        // 翻倍已经算在 RawDamage() 里（DamageMultiplier），这里不再自己乘。
        await DamageCmd.Attack(RawDamage())
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(6m);
}
