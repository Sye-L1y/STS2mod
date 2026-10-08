using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Patches.Localization;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MyCharMod.Mechanics;

/// <summary>
/// 「厄难覆盖」的载体：**消耗协议层数时**，每消耗 1 层就随机给一个敌人叠 N 层[gold]灾厄[/gold]
/// （N 由卡牌打出时经 <see cref="Configure"/> 写入，基础 3 / 升级 5）。
///
/// 监听 E17「协议消耗」事件——无论消耗来自回合结算还是协议消耗牌（歼灭龙息 / 紧急护壁 / 龙吼模块）。
///
/// **叠加**：触发条件完全一致（都是「协议消耗」），所以多张合并成一条，**数值相加**：
/// 基础 + 升级 = 每层消耗给 3+5=8 层灾厄。份数记在 <see cref="PowerModel.Amount"/>（状态栏数字即份数），
/// 累加值记在 <see cref="DynamicVars"/>["Doom"]，悬浮说明按累加后的值渲染。
/// </summary>
public sealed class CalamityCoveragePower : CustomPowerModel, IAlphaProtocolConsumeListener,
    IAddDumbVariablesToPowerDescription
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldScaleInMultiplayer => false;

    public override string? CustomPackedIconPath => MyCharAssets.Power("protocol");

    public override string? CustomBigIconPath => MyCharAssets.Power("protocol");

    /// <summary>初值 0：本值是「各份打出的数值之和」，由 <see cref="Configure"/> 累加，不是卡面值。</summary>
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Doom", 0m),
    };

    public override List<(string, string)>? Localization => new()
    {
        ("title", "厄难覆盖"),
        ("description", "消耗[gold]协议[/gold]时，每消耗1层，随机给予一个敌人{Doom}层[gold]灾厄[/gold]。"),
    };

    public void AddDumbVariablesToPowerDescription(LocString description) =>
        description.Add("Doom", DynamicVars["Doom"].IntValue);

    /// <summary>
    /// 由「厄难覆盖」卡牌在打出时写入本份的灾厄层数（本牌升级后更高）。
    /// 累加而非覆盖：多张打出时数值相加（触发条件一致，合并成一条）。
    /// </summary>
    public void Configure(int doom) => DynamicVars["Doom"].BaseValue += doom;

    public async Task OnAlphaProtocolConsumed(PlayerChoiceContext choiceContext, Player player, int layers)
    {
        if (layers <= 0 || player.Creature != Owner)
        {
            return;
        }

        ICombatState? combatState = Owner.CombatState;
        if (combatState == null)
        {
            return;
        }
        IReadOnlyList<Creature> enemies = combatState.HittableEnemies;
        if (enemies.Count == 0)
        {
            return;
        }

        decimal doom = DynamicVars["Doom"].BaseValue;
        for (int i = 0; i < layers; i++)
        {
            Creature? target = player.RunState.Rng.CombatTargets.NextItem(enemies);
            if (target == null)
            {
                return;
            }
            await PowerCmd.Apply<DoomPower>(choiceContext, target, doom, Owner, null);
        }
    }
}
