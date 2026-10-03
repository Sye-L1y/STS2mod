using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;

namespace MyCharMod;

/// <summary>
/// 起始攻击牌。类名 -> ModelId "card.MY_CHAR_STRIKE"，本地化 key 为 cards.MY_CHAR_STRIKE.title / .description。
/// [Pool] 让 BaseLib 自动把这张牌挂进 MyCharCardPool。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class MyCharStrike : CustomCardModel
{
    protected override HashSet<CardTag> CanonicalTags => new() { CardTag.Strike };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DamageVar(6m, ValueProp.Move),
    };

    /// <summary>卡图：assets/images/cards/strike.png（500x380）。</summary>
    public override string? CustomPortraitPath => MyCharAssets.Card("strike");

    public override List<(string, string)>? Localization => new CardLoc(
        "打击",
        "造成{Damage:diff()}点伤害。");

    public MyCharStrike()
        : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
    {
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}