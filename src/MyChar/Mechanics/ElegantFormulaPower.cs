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
/// 「优美公式」的载体：本场战斗中，所有**已完成迭代次数 ≥ 阈值**的迭代牌费用 −1。
/// 阈值由卡牌打出时经 <see cref="Configure"/> 写入（基础 3 / 升级 2）。
///
/// 走引擎的 <see cref="TryModifyEnergyCostInCombat"/>：与「本回合免费」（E5 的 Local 修正）互不干扰，
/// 也只对「玩家自己的迭代牌」生效。
///
/// **叠加**：<see cref="PowerStackType.Counter"/>，份数记在 <see cref="PowerModel.Amount"/>，
/// 每份再降 1 费（2 份即 -2），状态栏数字即份数。
/// </summary>
public sealed class ElegantFormulaPower : CustomPowerModel, IAddDumbVariablesToPowerDescription
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

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

    /// <summary>本牌重复打出的份数（Counter 叠层层数）：先打出的那张为 1。</summary>
    private int Copies => System.Math.Max(1, Amount);

    public void AddDumbVariablesToPowerDescription(LocString description)
    {
        description.Add("Threshold", DynamicVars["Threshold"].IntValue);
        description.Add("Reduction", Copies);
    }

    /// <summary>由「优美公式」卡牌在打出时写入阈值（本牌升级后更低）。</summary>
    public void Configure(int threshold) => DynamicVars["Threshold"].BaseValue = threshold;

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
        if (completed < DynamicVars["Threshold"].IntValue)
        {
            return false;
        }

        modifiedCost = Math.Max(0m, originalCost - Copies);
        return true;
    }
}