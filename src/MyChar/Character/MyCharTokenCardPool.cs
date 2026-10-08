using BaseLib.Abstracts;
using Godot;

namespace MyCharMod;

/// <summary>
/// α / β / γ 协议牌的专属池：它们是**衍生牌**——回合开始时按协议档位生成，不属于角色的常规卡池。
/// 放进角色池（<see cref="MyCharCardPool"/>）会被「技能药水」等「从角色池取技能牌」的效应开出来，
/// 所以单独成池。取牌类效应都走 <c>player.Character.CardPool</c>，因此这里不会被药水 / 奖励抽到。
///
/// IsShared = true 的原因：本池不是任何角色的 <c>CardPool</c>，必须靠它进 <c>ModelDb.AllCardPools</c>，
/// 否则 <c>CardModel.Pool</c> 找不到归属会抛 InvalidProgramException。
/// 注意 shared 只影响「全局池列表」（图鉴 / AllCards），不影响各角色的奖励与药水。
/// </summary>
public sealed class MyCharTokenCardPool : CustomCardPoolModel
{
    /// <summary>卡图/图集路径里会用到的池子名，只能用 [a-z0-9_]。</summary>
    public override string Title => "my_char_token";

    // 与角色主池一致：走 BaseLib 带分隔符的 EnergyColorName，用同一套自定义能量图标。

    public override string? BigEnergyIconPath => MyCharAssets.EnergyBig;

    public override string? TextEnergyIconPath => MyCharAssets.EnergyText;

    public override Color DeckEntryCardColor => new("D62000");

    public override Color EnergyOutlineColor => new("802020");

    public override Color ShaderColor => new("c4d8f6");

    public override bool IsColorless => false;

    public override bool IsShared => true;

    /// <summary>true = 图鉴里默认已解锁显示，不用先"见过"。</summary>
    public override bool SeenByDefault => true;
}