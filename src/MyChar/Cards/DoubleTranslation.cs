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
/// 稀有攻击「双重译制」：造成 8（12）伤害；[gold]响应[/gold]；
/// 本牌响应[gold]传参[/gold]的**伤害增加**效果翻倍（DamageBoost 百分比 / DamageAdd 平伤 /
/// α协议 EffectBoost 的伤害部分；格挡不翻倍）。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class DoubleTranslation : AlphaCard
{
    public override bool IsResponse => true;

    /// <summary>本牌响应传参的伤害增加效果翻倍。</summary>
    protected override decimal DamageParamMultiplier => 2m;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new AlphaDamageVar(8m, ValueProp.Move),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("double_translation");

    public override List<(string, string)>? Localization => new CardLoc(
        "双重译制",
        "造成{Damage:diff()}点伤害。\n[gold]响应[/gold]：本牌响应[gold]传参[/gold]的伤害增加效果翻倍。",
        ("flavor", "同一句话翻成两种说法，力道就翻了一倍。"));

    public DoubleTranslation()
        : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
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

    /// <summary>升级把基础伤害提到 12（不再翻倍传参效果）。</summary>
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4m);
}