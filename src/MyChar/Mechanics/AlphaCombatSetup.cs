using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace MyCharMod.Mechanics;

/// <summary>
/// 战斗内核心 Power 的挂载。起始遗物（普通版 / 古老版）在每个己方回合开始时调用，
/// 幂等：已挂上就不重复挂。
/// </summary>
internal static class AlphaCombatSetup
{
    /// <summary>确保「传参池」（<see cref="AlphaCorePower"/>）与「协议」（<see cref="ProtocolPower"/>）两个可见 Power 已挂上。</summary>
    public static async Task EnsureCorePowers(PlayerChoiceContext choiceContext, Player player)
    {
        if (!player.Creature.HasPower<AlphaCorePower>())
        {
            await PowerCmd.Apply<AlphaCorePower>(choiceContext, player.Creature, 1m, player.Creature, null);
        }
        if (!player.Creature.HasPower<ProtocolPower>())
        {
            await PowerCmd.Apply<ProtocolPower>(choiceContext, player.Creature, 1m, player.Creature, null);
        }
    }
}