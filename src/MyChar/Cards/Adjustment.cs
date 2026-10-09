using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 稀有能力「调节」：每回合**第一次**打出[gold]迭代[/gold]牌时，该牌回到手牌
/// （所有迭代牌共用这一次额度，见 docs/08 §0 规则 P4）。
///
/// 由 <see cref="AdjustmentPower"/> 记额度，真正的回手在
/// <see cref="AlphaCard.AfterCardPlayedLate"/> 里做——那里才是引擎把打出的牌移出出牌区之后的时机。
/// 2 费，升级减费（2 → 1）。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class Adjustment : AlphaCard
{
    public override string? CustomPortraitPath => MyCharAssets.Card("adjustment");

    public override List<(string, string)>? Localization => new CardLoc(
        "调节",
        "每回合第一次打出[gold]迭代[/gold]牌时，该牌回到手牌。",
        ("flavor", "输出不对就把信号绕回来，重跑一遍。"));

    public Adjustment()
        : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<AdjustmentPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
