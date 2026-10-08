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

/// <summary>普通攻击「数据注入」：造成 7 伤；[gold]传参[/gold]：响应牌伤害 +3（5）。</summary>
[Pool(typeof(MyCharCardPool))]
public sealed class DataInjection : AlphaCard
{
    public override bool IsParamSource => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new AlphaDamageVar(7m, ValueProp.Move),
        new DynamicVar("Param", 3m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("data_injection");

    public override List<(string, string)>? Localization => new CardLoc(
        "数据注入",
        "造成{Damage:diff()}点伤害。\n[gold]传参[/gold]：响应牌伤害+{Param:diff()}。",
        ("flavor", "这一击不只是伤害，还顺手写进了一条参数。"));

    public DataInjection()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy)
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
            Kind = ParamKind.DamageAdd,
            Value = DynamicVars["Param"].BaseValue,
            SourceId = "DATA_INJECTION",
        };

    protected override void OnUpgrade() => DynamicVars["Param"].UpgradeValueBy(2m);
}