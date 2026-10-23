using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Patches.Localization;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace MyCharMod.Mechanics;

/// <summary>
/// 「多层网络输出」的击杀奖励载体：战斗结束后从**牌组**里随机升级一张可升级的牌。
///
/// ★ **挂载时机**（v1.19 修）：本 Power 必须在**造成伤害之前**就挂上，不能等击杀确认再挂。
/// 击杀最后一个敌人时 <c>CombatManager.IsEnding</c> 会在伤害结算的瞬间变为 true
/// （判据是「场上没有存活的主敌人」），而 <c>PowerCmd.Apply</c> 开头就是
/// <c>if (CombatManager.Instance.IsEnding) return null;</c> —— 于是「刚好打死最后一个敌人
/// 从而结束战斗」时 Power 根本挂不上，战后奖励被静默丢掉。
/// 卡片打出的那一刻目标还活着（IsEnding = false），那时挂载是安全的；击杀确认后只调
/// <see cref="Arm"/>（直接改字段，不走命令，不受 IsEnding 影响）。
///
/// ★ 本 Power 是纯内部标记，**不上状态栏**（<see cref="IsVisibleInternal"/> 恒 false）：
/// 挂载时还无法预知会不会击杀，提前亮图标会误导。
///
/// 兑现走 <c>AfterCombatEnd</c>：引擎在 <c>CombatManager.EndCombatInternal</c> 里先派发
/// <c>Hook.AfterCombatEnd</c>，之后才 <c>player.AfterCombatEnd()</c> 清掉身上的 Power，
/// 所以本 Power 在战斗结束的那一刻还在场，能拿到这次升级。
/// （随机源与本体 / BaseLib 的随机升级奖励一致，用 <c>RunState.Rng.Niche</c>。）
/// </summary>
public sealed class MultiLayerOutputPower : CustomPowerModel
{
    public override PowerType Type => PowerType.Buff;

    /// <summary>开关型：一次战斗只需兑现一次升级，多份不叠。</summary>
    public override PowerStackType StackType => PowerStackType.Single;

    public override bool ShouldScaleInMultiplayer => false;

    /// <summary>纯内部标记，不在状态栏出现。</summary>
    protected override bool IsVisibleInternal => false;

    public override string? CustomPackedIconPath => MyCharAssets.Power("multi_layer_output");

    public override string? CustomBigIconPath => MyCharAssets.Power("multi_layer_output");

    public override List<(string, string)>? Localization => new()
    {
        ("title", "多层网络输出"),
        ("description", "战斗结束后，随机升级一张牌。"),
    };

    /// <summary>是否已确认击杀。打了但没打死时不兑现升级。</summary>
    private bool _armed;

    /// <summary>
    /// 击杀确认后由卡片调用：点燃标记，战斗结束时兑现升级。
    /// 只改字段、不走 <c>PowerCmd</c> —— 此刻战斗往往已经进入收尾（IsEnding = true），命令会被拦掉。
    /// </summary>
    public void Arm() => _armed = true;

    public override Task AfterCombatEnd(CombatRoom room)
    {
        if (!_armed)
        {
            return Task.CompletedTask;
        }
        _armed = false;

        if (Owner.Player is not { } player)
        {
            return Task.CompletedTask;
        }

        List<CardModel> candidates = player.Deck.Cards
            .Where(card => card.IsUpgradable)
            .ToList();
        CardModel? pick = player.RunState.Rng.Niche.NextItem(candidates);
        if (pick != null)
        {
            CardCmd.Upgrade(pick);
        }
        return Task.CompletedTask;
    }
}
