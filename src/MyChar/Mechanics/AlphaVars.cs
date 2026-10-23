using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace MyCharMod.Mechanics;

/// <summary>
/// 阿尔法的伤害变量。卡面预览与实际结算走**同一条口径**：
///   原始值 = floor((基础值 + 迭代成长) × 传参增幅)   ← 见 <see cref="AlphaCard.RawDamage"/>
///   最终值 = Hook.ModifyDamage(原始值)                ← 力量 / 活力等由引擎在这一步加上
/// 预览走 <c>Hook.ModifyDamage</c>，实战的 <c>CreatureCmd.Damage</c> 也走 <c>Hook.ModifyDamage</c>，
/// 所以卡面数字（含力量 / 活力）与实际打出的伤害永远一致；
/// 传参增幅只乘在基础值上，不会把力量 / 活力也一起放大（旧版正是这里对不上）。
/// </summary>
internal sealed class AlphaDamageVar : DamageVar
{
    /// <summary>true = 本牌主伤害（含迭代与传参增幅）；false = 附加伤害（如「定点执行」的 Bonus），只挂力量 / 活力。</summary>
    private readonly bool _main;

    public AlphaDamageVar(decimal baseValue, ValueProp props)
        : base(baseValue, props)
    {
        _main = true;
    }

    public AlphaDamageVar(string name, decimal baseValue, ValueProp props)
        : base(name, baseValue, props)
    {
        _main = false;
    }

    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target,
        bool runGlobalHooks)
    {
        // 预览时把目标传进去：像「叠仇」这种按目标负面状态算额外伤害的牌，卡面才能显示实战值。
        decimal raw = _main && card is AlphaCard alpha ? alpha.RawDamage(target) : BaseValue;
        if (!runGlobalHooks)
        {
            PreviewValue = raw;
            return;
        }

        ICombatState? combatState = card.CombatState ?? card.Owner.Creature.CombatState;
        PreviewValue = Hook.ModifyDamage(card.Owner.RunState, combatState, target, card.Owner.Creature,
            raw, Props, card, null, ModifyDamageHookType.All, previewMode, out IEnumerable<AbstractModel> _);
        PreviewValue = Math.Max(PreviewValue, 0m);
    }
}

/// <summary>
/// 阿尔法的格挡变量，与 <see cref="AlphaDamageVar"/> 同理：
/// 预览 = <c>Hook.ModifyBlock</c>(原始值)，实战 <c>CreatureCmd.GainBlock</c> 也走 <c>Hook.ModifyBlock</c>，两者一致。
/// </summary>
internal sealed class AlphaBlockVar : BlockVar
{
    public AlphaBlockVar(decimal block, ValueProp props)
        : base(block, props)
    {
    }

    public override void UpdateCardPreview(CardModel card, CardPreviewMode previewMode, Creature? target,
        bool runGlobalHooks)
    {
        decimal raw = card is AlphaCard alpha ? alpha.RawBlock() : BaseValue;
        if (!runGlobalHooks)
        {
            PreviewValue = raw;
            return;
        }

        PreviewValue = Hook.ModifyBlock(card.CombatState!, card.Owner.Creature, raw, Props, card, null,
            out IEnumerable<AbstractModel> _);
        PreviewValue = Math.Max(PreviewValue, 0m);
    }
}