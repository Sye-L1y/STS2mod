using System;
using System.Collections.Generic;
using BaseLib.Abstracts;
using BaseLib.Patches.Localization;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;

namespace MyCharMod.Mechanics;

/// <summary>
/// 「优美公式」的载体：本场战斗中，所有**已完成迭代次数 ≥ 阈值**的迭代牌费用 −N（N = 本实例归并的份数）。
///
/// 走引擎的 <see cref="TryModifyEnergyCostInCombat"/>：与「本回合免费」（E5 的 Local 修正）互不干扰，
/// 也只对「玩家自己的迭代牌」生效。
///
/// **叠加**：门槛不同（基础 3 / 升级 2）则触发条件不同，不能合并 —— 所以本 Power 是
/// <see cref="PowerInstanceType.Instanced"/>，由卡牌在打出时按「门槛」自己归并：
/// 同门槛的份数加在同一个实例上（<see cref="AddCopy"/>），门槛不同就各占一条状态栏。
/// 状态栏数字 = 该门槛下已打出的份数 = 这些迭代牌的降费额度。
///
/// 注意：实例化后<b>必须</b>用 <c>Creature.GetPowerInstances&lt;T&gt;()</c> 找载体，
/// <c>GetPower&lt;T&gt;()</c> 只会返回第一个实例（会把参数写到别的门槛身上）。
/// </summary>
public sealed class ElegantFormulaPower : CustomPowerModel, IAddDumbVariablesToPowerDescription
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public override bool ShouldScaleInMultiplayer => false;

    public override string? CustomPackedIconPath => MyCharAssets.Power("param_iteration");

    public override string? CustomBigIconPath => MyCharAssets.Power("param_iteration");

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Threshold", 3m),
    };

    public override List<(string, string)>? Localization => new()
    {
        ("title", "优美公式"),
        ("description", "本场战斗中，所有已完成[gold]迭代[/gold]次数≥{Threshold}的[gold]迭代[/gold]牌费用-{Reduction}。"),
    };

    /// <summary>本实例归并的份数（同门槛的牌一共打了几张）：先打出的那张为 1。</summary>
    private int Copies => Math.Max(1, Amount);

    /// <summary>本实例的门槛（基础牌 3 / 升级牌 2），卡牌打出时经 <see cref="Configure"/> 写入。</summary>
    public int IterationThreshold => DynamicVars["Threshold"].IntValue;

    public void AddDumbVariablesToPowerDescription(LocString description)
    {
        description.Add("Threshold", IterationThreshold);
        description.Add("Reduction", Copies);
    }

    /// <summary>由「优美公式」卡牌在打出时写入本实例的门槛（升级后更低）。</summary>
    public void Configure(int threshold) => DynamicVars["Threshold"].BaseValue = threshold;

    /// <summary>同门槛再打出一份：份数 +1（不新建实例，降费额度随之 +1）。</summary>
    public void AddCopy() => SetAmount(Amount + 1);

    public override bool TryModifyEnergyCostInCombat(CardModel card, decimal originalCost, out decimal modifiedCost)
    {
        modifiedCost = originalCost;

        if (card is not AlphaCard { IterationEvery: > 0 } alpha)
        {
            return false;
        }
        if (card.Owner?.Creature != Owner)
        {
            return false;
        }

        Player? player = Owner.Player;
        AlphaCorePower? core = player == null ? null : AlphaCorePower.Of(player);
        if (core == null)
        {
            return false;
        }

        int completed = core.GetIterationStacks(card, alpha.IterationEvery);
        if (completed < IterationThreshold)
        {
            return false;
        }

        modifiedCost = Math.Max(0m, originalCost - Copies);
        return true;
    }
}
