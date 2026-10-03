using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.PotionPools;

namespace MyCharMod;

/// <summary>
/// 角色的药水池。
///
/// 占位阶段直接复用铁甲战士的药水清单，保证药水掉落不会空。
/// 之后要做专属药水时，把 GenerateAllPotions 换成自己的药水，并去掉对 IroncladPotionPool 的引用。
///
/// 类名 -> ModelId "potion_pool.MY_CHAR_POTION_POOL"
/// </summary>
public sealed class MyCharPotionPool : CustomPotionPoolModel
{
    public override string EnergyColorName => "ironclad";

    protected override IEnumerable<PotionModel> GenerateAllPotions()
        => ModelDb.PotionPool<IroncladPotionPool>().AllPotions;
}