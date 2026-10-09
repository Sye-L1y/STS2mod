using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 罕见攻击「攻守兼备」：造成 8（10）点伤害；
/// [gold]传参[/gold]：响应牌**造成伤害时获得等量格挡**。
///
/// 传参本身没有即时效果（只打标记），响应牌在本次结算里造成的伤害由
/// <see cref="AlphaCard.AfterDamageGiven"/> 逐笔累加，打完一次性换成等量格挡（见 AlphaCard 的 2.6 步）。
/// 口径同 P9：只作用于**消费它的那张响应牌本次结算**，不写回卡牌、不留存。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class OffenseDefense : AlphaCard
{
    public override bool IsParamSource => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new AlphaDamageVar(8m, ValueProp.Move),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("offense_defense");

    public override List<(string, string)>? Localization => new CardLoc(
        "攻守兼备",
        "造成{Damage:diff()}点伤害。\n[gold]传参[/gold]：响应牌造成伤害时获得等量格挡。",
        ("flavor", "打出去多少，就挡回来多少。"));

    public OffenseDefense()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(RawDamage())
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override ParamEntry? BuildParam() =>
        new()
        {
            Kind = ParamKind.BlockFromDamage,
            Value = 1m,
            SourceId = "OFFENSE_DEFENSE",
        };

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(2m);
}
