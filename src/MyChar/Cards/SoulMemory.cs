using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 稀有技能「灵魂记忆」：将一张「灵魂」放入手牌；
/// 从抽牌堆选择 2（3）张带[gold]传参[/gold]的牌，**触发它们的传参**。
///
/// 「触发其传参」= 把这些牌本来会抛出的传参**入池**（不打出它们），由下一张响应牌照常消费。
/// 走 <see cref="AlphaCard.ParamsForTrigger"/> 读取条目，统一经 <c>AddParamWithHook</c> 入池。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class SoulMemory : AlphaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Pick", 2m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("soul_memory");

    public override List<(string, string)>? Localization => new CardLoc(
        "灵魂记忆",
        "将一张「灵魂」放入手牌。从抽牌堆选择{Pick:diff()}张带[gold]传参[/gold]的牌，触发它们的[gold]传参[/gold]。",
        ("flavor", "记忆不会消失，它只是换了个地方等着被读出来。"),
        ("selectionScreenPrompt", "选择 {Amount} 张带传参的牌"));

    public SoulMemory()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ICombatState? combatState = CombatState ?? Owner.Creature.CombatState;
        if (combatState == null)
        {
            return;
        }
        await Soul.CreateInHand(Owner, 1, combatState, Owner);

        CardPile drawPile = PileType.Draw.GetPile(Owner);
        int count = Math.Min((int)DynamicVars["Pick"].BaseValue, drawPile.Cards.Count(IsParamCard));
        if (count <= 0)
        {
            return;
        }

        IEnumerable<CardModel> picked = await CardSelectCmd.FromCombatPile(choiceContext, drawPile, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, count), IsParamCard);

        AlphaCorePower? core = AlphaCorePower.Of(this);
        if (core == null)
        {
            return;
        }
        core.BeginParamBatch();
        foreach (CardModel card in picked.ToList())
        {
            if (card is not AlphaCard alpha)
            {
                continue;
            }
            foreach (ParamEntry entry in alpha.ParamsForTrigger())
            {
                await core.AddParamWithHook(choiceContext, entry);
            }
        }
    }

    /// <summary>带传参的牌（<see cref="AlphaCard.IsParamSource"/>）。</summary>
    private static bool IsParamCard(CardModel card) => card is AlphaCard { IsParamSource: true };

    protected override void OnUpgrade() => DynamicVars["Pick"].UpgradeValueBy(1m);
}
