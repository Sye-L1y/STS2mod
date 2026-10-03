using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MyCharMod.Mechanics;

/// <summary>
/// 「龙吼模块」的临时力量载体（E15）：回合结束自动扣回等量力量。
/// 复用本体的 <see cref="TemporaryStrengthPower"/> 管线，只需指明来源牌。
/// </summary>
public sealed class DragonRoarTempStrengthPower : TemporaryStrengthPower
{
    public override AbstractModel OriginModel => ModelDb.Card<ProtocolExtract>();
}