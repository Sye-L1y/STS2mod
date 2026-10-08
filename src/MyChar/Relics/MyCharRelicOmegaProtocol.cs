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
using MegaCrit.Sts2.Core.ValueProps;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 稀有遗物「欧米伽协议」：每场战斗中，首次**集齐**打出 α、β、γ 三种协议牌后，对所有敌人造成 50 点伤害。
/// 「首次」按**每场战斗一次**（状态在 <see cref="AlphaCorePower"/> 的战斗内数据里，随战斗重置）。
/// </summary>
[Pool(typeof(MyCharRelicPool))]
public sealed class MyCharRelicOmegaProtocol : CustomRelicModel, IAlphaProtocolListener
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override string IconBaseName => "anchor";

    public override List<(string, string)>? Localization => new()
    {
        ("title", "欧米伽协议"),
        ("description", "每场战斗中，首次打出[gold]α、β、γ三种协议[/gold]后，对所有敌人造成50点伤害。"),
        ("flavor", "最终授权，只兑现一次。"),
    };

    public async Task OnAlphaProtocol(PlayerChoiceContext choiceContext, CardModel card)
    {
        AlphaCorePower? core = AlphaCorePower.Of(Owner);
        if (core == null || core.Core.OmegaTriggered)
        {
            return;
        }

        // 记录本场战斗已打出的协议种类；只有 α/β/γ 三种都出现过才触发。
        string? kind = card switch
        {
            ProtocolAlpha => "alpha",
            ProtocolBeta => "beta",
            ProtocolGamma => "gamma",
            _ => null,
        };
        if (kind == null)
        {
            return;
        }
        core.Core.PlayedProtocols.Add(kind);
        if (core.Core.PlayedProtocols.Count < 3)
        {
            return;
        }
        core.Core.OmegaTriggered = true;

        ICombatState? combatState = Owner.Creature.CombatState;
        if (combatState == null)
        {
            return;
        }

        Flash();
        foreach (Creature enemy in combatState.HittableEnemies)
        {
            await CreatureCmd.Damage(choiceContext, enemy, 50m, ValueProp.Unpowered, (CardModel?)null, null);
        }
    }
}