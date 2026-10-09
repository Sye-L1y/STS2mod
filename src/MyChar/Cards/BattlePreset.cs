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
/// 罕见攻击「战斗预设」：造成 14（18）点伤害；
/// [gold]传参[/gold]：响应牌伤害增加**本牌造成的伤害**（把本牌本次打出的伤害值写进一条 <see cref="ParamKind.DamageAdd"/>）。
///
/// 打出顺序是「响应 → 本牌效果 → 传参 → 迭代」，所以在 <see cref="PlayEffect"/> 里先把本牌伤害快照下来，
/// 到 <see cref="BuildParam"/> 时直接用它，避免 <c>_resolving</c> 已经还原、取到预览值。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class BattlePreset : AlphaCard
{
    public override bool IsParamSource => true;

    /// <summary>本牌本次打出的伤害快照（供传参取用）。</summary>
    private decimal _dealtDamage;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new AlphaDamageVar(14m, ValueProp.Move),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("battle_preset");

    public override List<(string, string)>? Localization => new CardLoc(
        "战斗预设",
        "造成{Damage:diff()}点伤害。\n[gold]传参[/gold]：响应牌伤害增加本牌造成的伤害。",
        ("flavor", "预设好路径，下一次出手只是执行。"));

    public BattlePreset()
        : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        _dealtDamage = RawDamage();
        await DamageCmd.Attack(_dealtDamage)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override ParamEntry? BuildParam() =>
        new()
        {
            Kind = ParamKind.DamageAdd,
            Value = _dealtDamage,
            SourceId = "BATTLE_PRESET",
        };

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4m);
}
