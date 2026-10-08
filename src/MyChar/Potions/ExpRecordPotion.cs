using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 罕见专属药水「经验记录」：使手牌中一张[gold]迭代[/gold]牌的计数器 +6。
/// 计数走「已打出次数」，所以间隔 X &gt; 1 时 +6 只是推进进度；
/// 跨过的迭代档位由 <see cref="AlphaCorePower.AdvanceIteration"/> 立即补结算。
/// </summary>
[Pool(typeof(MyCharPotionPool))]
public sealed class ExpRecordPotion : CustomPotionModel
{
    public override PotionRarity Rarity => PotionRarity.Uncommon;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.AnyPlayer;

    public override string? CustomPackedImagePath => MyCharAssets.Potion("exp_record");

    public override string? CustomLargeImagePath => MyCharAssets.Potion("exp_record");

    public override List<(string, string)>? Localization => new PotionLoc(
        "经验记录",
        "使手牌中一张[gold]迭代[/gold]牌的计数器+6。",
        ("selectionScreenPrompt", "选择1张迭代牌"));

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        PotionModel.AssertValidForTargetedPotion(target);
        Player player = Owner;
        IEnumerable<CardModel> picked = await CardSelectCmd.FromHand(choiceContext, player,
            new CardSelectorPrefs(SelectionScreenPrompt, 1), IsIterationCard, this);
        CardModel? card = picked.FirstOrDefault();
        if (card == null)
        {
            return;
        }

        AlphaCorePower? core = AlphaCorePower.Of(player);
        if (core != null)
        {
            await core.AdvanceIteration(choiceContext, card, 6);
        }
    }

    private static bool IsIterationCard(CardModel card) => card is AlphaCard { IterationEvery: > 0 };
}