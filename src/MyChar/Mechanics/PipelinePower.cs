using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Hooks;
using BaseLib.Patches.Localization;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace MyCharMod.Mechanics;

/// <summary>
/// 「流水线」的载体：己方回合开始时额外抽 N 张牌，同时手牌上限 −N。
///
/// 手牌上限走 BaseLib 的 <see cref="IMaxHandSizeModifier"/> 钩子——BaseLib 在
/// <c>CardPile.MaxCardsInHand</c> 处打补丁，遍历 <c>CombatState.IterateHookListeners()</c>
/// （其中包含 <c>creature.Powers</c>），所以挂在自己身上的本 Power 能被回调到。
/// 抽牌走 <c>AfterPlayerTurnStart</c>（与「待发抽牌」同款），正好在基础抽牌之后再补 N 张。
///
/// **叠加**：<see cref="PowerStackType.Counter"/>，份数记在 <see cref="PowerModel.Amount"/>。
/// </summary>
public sealed class PipelinePower : CustomPowerModel, IMaxHandSizeModifier, IAddDumbVariablesToPowerDescription
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldScaleInMultiplayer => false;

    public override string? CustomPackedIconPath => MyCharAssets.Power("pipeline");

    public override string? CustomBigIconPath => MyCharAssets.Power("pipeline");

    public override List<(string, string)>? Localization => new()
    {
        ("title", "流水线"),
        ("description", "每回合开始时额外抽{Draw}张牌；手牌上限−{Draw}。"),
    };

    /// <summary>本牌重复打出的份数（Counter 叠层层数）：先打出的那张为 1。</summary>
    private int Copies => System.Math.Max(1, Amount);

    public void AddDumbVariablesToPowerDescription(LocString description) =>
        description.Add("Draw", Copies);

    public int ModifyMaxHandSize(Player player, int currentMaxHandSize) =>
        player.Creature == Owner ? currentMaxHandSize - Copies : currentMaxHandSize;

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner)
        {
            return;
        }
        await CardPileCmd.Draw(choiceContext, Copies, player);
    }
}
