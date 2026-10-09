using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 罕见技能「语义附加」：**消耗**；使一张**没有响应**的手牌获得[gold]响应[/gold]。
///
/// 授予是**本场战斗**内的（只挂在手牌的那个卡牌实例上，战斗结束牌回牌组即自然失效），
/// 不写回牌组实体。判定统一走 <see cref="AlphaCard.HasResponse"/>（= 天生带响应或本场被授予）。
/// 只对**阿尔法机自己的牌**开放（<see cref="AlphaCard"/> 之外的牌没有传参池可消费，给了响应也没意义）。
/// 1 费，升级减费（1 → 0）。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class SemanticAttach : AlphaCard
{
    protected override IEnumerable<CardKeyword> BaseKeywords => new[] { CardKeyword.Exhaust };

    public override string? CustomPortraitPath => MyCharAssets.Card("semantic_attach");

    public override List<(string, string)>? Localization => new CardLoc(
        "语义附加",
        "[gold]消耗[/gold]。使一张没有[gold]响应[/gold]的手牌获得[gold]响应[/gold]。",
        ("flavor", "给一个名词补上谓语，它就开始动了。"),
        ("selectionScreenPrompt", "选择 1 张没有响应的牌"));

    public SemanticAttach()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        List<CardModel> hand = PileType.Hand.GetPile(Owner).Cards.Where(IsAttachable).ToList();
        if (hand.Count == 0)
        {
            // 没有合法目标：照常打出（消耗），什么也不发生。
            return;
        }

        IEnumerable<CardModel> picked = await CardSelectCmd.FromHand(choiceContext, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1), IsAttachable, this);
        foreach (CardModel card in picked.ToList())
        {
            (card as AlphaCard)?.GrantResponse();
        }
    }

    /// <summary>可作为目标的牌：自己的、本场还没有响应的阿尔法机牌（不含本牌自己）。</summary>
    private bool IsAttachable(CardModel card) =>
        card != this && card is AlphaCard alpha && !alpha.HasResponse;

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
