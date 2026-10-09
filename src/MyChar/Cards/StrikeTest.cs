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
/// 罕见攻击「击打测试」：造成 9（12）伤；[gold]传参[/gold]：响应牌下一次打出**回到手牌**。
/// 「回手」是一次性传参（<see cref="ParamKind.ReturnToHand"/>）：消费它的那张响应牌打完回到手牌，
/// 条目本身随池清空消失，不写在牌上。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class StrikeTest : AlphaCard
{
    public override bool IsParamSource => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new AlphaDamageVar(9m, ValueProp.Move),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("strike_test");

    public override List<(string, string)>? Localization => new CardLoc(
        "击打测试",
        "造成{Damage:diff()}点伤害。\n[gold]传参[/gold]：响应牌下一次打出回到手牌。",
        ("flavor", "先敲一下，看看下一次响应会不会弹回来。"));

    public StrikeTest()
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
            Kind = ParamKind.ReturnToHand,
            Value = 1m,
            SourceId = "STRIKE_TEST",
        };

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
