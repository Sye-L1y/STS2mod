using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 罕见遗物「加速器」：每回合首次完成迭代时获得 1 点能量。
/// 「本回合是否已领过」的状态放在 <see cref="AlphaCorePower"/> 的战斗内数据里，回合开始重置。
/// </summary>
[Pool(typeof(MyCharRelicPool))]
public sealed class MyCharRelicAccelerator : CustomRelicModel, IAlphaIterationListener
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override string IconBaseName => "anchor";

    public override List<(string, string)>? Localization => new()
    {
        ("title", "加速器"),
        ("description", "每回合首次完成[gold]迭代[/gold]时，获得1点[gold]能量[/gold]。"),
        ("flavor", "第一次循环闭合的瞬间，能量自己涌了出来。"),
    };

    public async Task OnAlphaIteration(PlayerChoiceContext choiceContext, CardModel card)
    {
        AlphaCorePower? core = AlphaCorePower.Of(Owner);
        if (core == null || core.Core.AcceleratorUsedThisTurn)
        {
            return;
        }
        core.Core.AcceleratorUsedThisTurn = true;
        Flash();
        await PlayerCmd.GainEnergy(1m, Owner);
    }

    public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (participants.Contains(Owner.Creature))
        {
            AlphaCorePower? core = AlphaCorePower.Of(Owner);
            if (core != null)
            {
                core.Core.AcceleratorUsedThisTurn = false;
            }
        }
        return Task.CompletedTask;
    }
}