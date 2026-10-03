using System;
using System.Collections.Generic;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace MyCharMod.Mechanics;

/// <summary>
/// 自定义迭代变量的卡面显示。
///
/// 「伤害 / 格挡」已经由 <see cref="AlphaDamageVar"/> / <see cref="AlphaBlockVar"/> 统一处理
/// （原始值 = 基础值 + 迭代成长，再乘增幅，最后过 Hook.ModifyDamage / ModifyBlock），
/// 与实战口径完全一致，所以这里**不再碰**这两个变量。
///
/// 这里只补**其它**声明的迭代变量（如协议层数）：把「迭代层数 × 每次增量」加到对应动态变量上，
/// 再按池里的增幅向下取整，和 <see cref="AlphaCard.ResolveValue"/> 对得上。
///
/// PreviewValue 是纯显示值：不参与实际结算，且 UpdateCardPreview 每次都会用 BaseValue 重算，
/// 所以这里既不会污染真实数值，也不会跨次调用累积、跨战斗残留。
/// </summary>
[HarmonyPatch(typeof(CardModel), "UpdateDynamicVarPreview")]
internal static class AlphaPreviewPatch
{
    [HarmonyPostfix]
    private static void Postfix(CardModel __instance, DynamicVarSet dynamicVarSet)
    {
        if (__instance is not AlphaCard card)
        {
            return;
        }

        int stacks = card.IterationStacks;

        foreach (KeyValuePair<string, decimal> pair in card.IterationVarBonus)
        {
            string name = pair.Key;
            if (name is "Damage" or "Block")
            {
                continue;
            }
            if (!dynamicVarSet.TryGetValue(name, out DynamicVar? variable))
            {
                continue;
            }

            if (stacks > 0)
            {
                variable.PreviewValue += pair.Value * stacks;
            }

            decimal boost = card.PreviewBoostFor(name);
            if (boost != 1m)
            {
                variable.PreviewValue = Math.Floor(variable.PreviewValue * boost);
            }
        }
    }
}