using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 普通专属药水「临时协议」：将一张 β协议 加入手牌。
/// 协议矩阵在场时，新生成的协议牌照常自动升级（走 <see cref="ProtocolMatrixPower.UpgradeNewProtocol"/>）。
/// </summary>
[Pool(typeof(MyCharPotionPool))]
public sealed class TempProtocolPotion : CustomPotionModel
{
    public override PotionRarity Rarity => PotionRarity.Common;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.AnyPlayer;

    public override string? CustomPackedImagePath => MyCharAssets.Potion("temp_protocol");

    public override string? CustomLargeImagePath => MyCharAssets.Potion("temp_protocol");

    public override List<(string, string)>? Localization => new PotionLoc(
        "临时协议",
        "将1张[gold]β协议[/gold]加入手牌。");

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        PotionModel.AssertValidForTargetedPotion(target);
        ICombatState? combatState = Owner.Creature.CombatState;
        if (combatState == null)
        {
            return;
        }

        CardModel card = combatState.CreateCard<ProtocolBeta>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, Owner);
        ProtocolMatrixPower.UpgradeNewProtocol(card, Owner);
    }
}