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
/// 「终响激发」的载体：
///   1. **传参入池**时，所有敌人本回合失去 N 点[gold]力量[/gold]（N 基础 1 / 升级 2，走 `PiercingWailPower`）；
///   2. 触发[gold]响应[/gold]时，所有敌人获得 M 层[gold]灾厄[/gold]（M 基础 5 / 升级 7）。
///
/// 监听 E18「传参入池」与 E2「响应触发」两个事件。数值由卡牌打出时经 <see cref="Configure"/> 写入。
///
/// **叠加**：<see cref="PowerStackType.Counter"/>，份数记在 <see cref="PowerModel.Amount"/>，
/// 失力量与灾厄数值均 × 份数，状态栏数字即份数。
/// </summary>
public sealed class FinalEchoStimulusPower : CustomPowerModel, IAlphaParamListener, IAlphaResponseListener,
    IAddDumbVariablesToPowerDescription
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldScaleInMultiplayer => false;

    public override string? CustomPackedIconPath => MyCharAssets.Power("param_iteration");

    public override string? CustomBigIconPath => MyCharAssets.Power("param_iteration");

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("StrengthLoss", 1m),
        new DynamicVar("Doom", 5m),
    };

    public override List<(string, string)>? Localization => new()
    {
        ("title", "终响激发"),
        ("description", "[gold]传参[/gold]时，所有敌人本回合失去{StrengthLoss}点[gold]力量[/gold]；触发[gold]响应[/gold]时，所有敌人获得{Doom}层[gold]灾厄[/gold]。"),
    };

    /// <summary>本牌重复打出的份数（Counter 叠层层数）：先打出的那张为 1。</summary>
    private int Copies => System.Math.Max(1, Amount);

    public void AddDumbVariablesToPowerDescription(LocString description)
    {
        description.Add("StrengthLoss", DynamicVars["StrengthLoss"].IntValue * Copies);
        description.Add("Doom", DynamicVars["Doom"].IntValue * Copies);
    }

    /// <summary>由「终响激发」卡牌在打出时写入失力量与灾厄数值（本牌升级后更高）。</summary>
    public void Configure(int strengthLoss, int doom)
    {
        DynamicVars["StrengthLoss"].BaseValue = strengthLoss;
        DynamicVars["Doom"].BaseValue = doom;
    }

    public async Task OnAlphaParamAdded(PlayerChoiceContext choiceContext, Player player, ParamEntry entry)
    {
        if (player.Creature != Owner)
        {
            return;
        }
        ICombatState? combatState = Owner.CombatState;
        if (combatState == null)
        {
            return;
        }

        decimal loss = DynamicVars["StrengthLoss"].BaseValue * Copies;
        foreach (Creature enemy in combatState.HittableEnemies)
        {
            await PowerCmd.Apply<PiercingWailPower>(choiceContext, enemy, loss, Owner, null);
        }
    }

    public async Task OnAlphaResponse(PlayerChoiceContext choiceContext, CardModel card)
    {
        Player? player = Owner.Player;
        ICombatState? combatState = Owner.CombatState;
        if (player == null || player.Creature != Owner || combatState == null)
        {
            return;
        }

        decimal doom = DynamicVars["Doom"].BaseValue * Copies;
        foreach (Creature enemy in combatState.HittableEnemies)
        {
            await PowerCmd.Apply<DoomPower>(choiceContext, enemy, doom, Owner, null);
        }
    }
}