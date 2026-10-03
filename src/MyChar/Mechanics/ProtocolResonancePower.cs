using System.Collections.Generic;
using BaseLib.Abstracts;
using BaseLib.Patches.Localization;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Localization;

namespace MyCharMod.Mechanics;

/// <summary>
/// 「协议共振」的载体：**被动增幅**——每当获得[gold]协议[/gold]，额外追加 <c>Amount</c> 层。
///
/// 不再自己产出协议（旧版「每回合开始时 +N」已移除），改成放大所有协议来源：
/// 卡牌 / 遗物 / 里程碑的每次 <see cref="ProtocolPower.AddProtocol"/> 都会带上这份加成。
/// 层数走 Amount，可叠加（打两张 = 每次多 +2）。
/// </summary>
public sealed class ProtocolResonancePower : CustomPowerModel, IAddDumbVariablesToPowerDescription
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldScaleInMultiplayer => false;

    public override string? CustomPackedIconPath => MyCharAssets.Power("protocol_resonance");

    public override string? CustomBigIconPath => MyCharAssets.Power("protocol_resonance");

    public override List<(string, string)>? Localization => new()
    {
        ("title", "协议共振"),
        ("description", "每次获得[gold]协议[/gold]时，额外获得{Amount}层。"),
    };

    public void AddDumbVariablesToPowerDescription(LocString description) =>
        description.Add("Amount", Amount);
}