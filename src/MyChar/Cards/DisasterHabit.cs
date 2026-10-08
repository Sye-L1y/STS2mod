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
/// 稀有攻击「灾难习性」：造成 18（21）伤害；[gold]响应[/gold]；
/// 触发[gold]响应[/gold]时，每条消耗的[gold]传参[/gold]再造成一次伤害。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class DisasterHabit : AlphaCard
{
    public override bool IsResponse => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new AlphaDamageVar(18m, ValueProp.Move),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("point_execution");

    public override List<(string, string)>? Localization => new CardLoc(
        "灾难习性",
        "造成{Damage:diff()}点伤害。\n[gold]响应[/gold]：触发[gold]响应[/gold]时，每条消耗的[gold]传参[/gold]再造成一次伤害。",
        ("flavor", "每一份被消耗的参数，都要用一次痛感来记账。"));

    public DisasterHabit()
        : base(3, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        await Strike();
        if (!ResponseConsumed)
        {
            return;
        }

        int extra = AlphaCorePower.Of(this)?.LastConsumed.Count ?? 0;
        for (int i = 0; i < extra; i++)
        {
            await Strike();
        }

        async Task Strike() =>
            await DamageCmd.Attack(RawDamage())
                .FromCard(this, cardPlay)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}