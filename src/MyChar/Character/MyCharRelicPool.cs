using BaseLib.Abstracts;

namespace MyCharMod;

/// <summary>
/// 角色的遗物池。遗物通过类上的 [Pool(typeof(MyCharRelicPool))] 自动挂进来。
/// 类名 -> ModelId "relic_pool.MY_CHAR_RELIC_POOL"
/// </summary>
public sealed class MyCharRelicPool : CustomRelicPoolModel
{
    /// <summary>占位阶段复用铁甲战士的能量图标。</summary>
    public override string EnergyColorName => "ironclad";

    public override bool SeenByDefault => true;
}