using System.Collections.Generic;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Models;

namespace MyCharMod;

/// <summary>
/// 角色的专属药水池。只放阿尔法专属的三瓶药水：
///   普通「临时协议」/ 罕见「经验记录」/ 稀有「特别参数」。
///
/// 药水不需要在这里手写清单：每瓶药水类上的 [Pool(typeof(MyCharPotionPool))] 会让
/// 基类构造函数把它们登记进 <c>ModHelper</c>，<c>PotionPoolModel.AllPotions</c> 会自动
/// 把登记过的药水拼到 <see cref="GenerateAllPotions"/> 的结果后面（见 CustomPotionPoolModel）。
/// 本体的通用药水池（SharedPotionPool）由 <c>PotionFactory.GetPotionOptions</c> 另行拼接，
/// 不归本池管。
///
/// 类名 -> ModelId "potion_pool.MY_CHAR_POTION_POOL"
/// </summary>
public sealed class MyCharPotionPool : CustomPotionPoolModel
{
    public override string EnergyColorName => "ironclad";

    /// <summary>图鉴里默认已解锁显示（与卡池口径一致）。</summary>
    public override bool SeenByDefault => true;

    protected override IEnumerable<PotionModel> GenerateAllPotions()
        => System.Array.Empty<PotionModel>();
}