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
/// 用 <c>AfterCombatEnd</c>：引擎在 <c>CombatManager.EndCombatInternal</c> 里先派发
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

    public override string? CustomPackedIconPath => MyCharAssets.Power("multi_layer_output");

    public override string? CustomBigIconPath => MyCharAssets.Power("multi_layer_output");

    public override List<(string, string)>? Localization => new()
    {
        ("title", "多层网络输出"),
        ("description", "战斗结束后，随机升级一张牌。"),
    };

    public override Task AfterCombatEnd(CombatRoom room)
    {
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
