using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.ValueProps;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 罕见攻击「战斗预设」：造成 14（18）点伤害；
/// [gold]传参[/gold]：响应牌伤害增加**本牌本次造成的未被格挡伤害**（把该数值写进一条
/// <see cref="ParamKind.DamageAdd"/>）。
///
/// ★ 传的是**实战结果**，不是卡面值：力量 / 活力加成、目标格挡、减伤等全部如实反映。
/// 口径 = <c>TotalDamage − BlockedDamage</c>（v1.24 起；<c>UnblockedDamage</c> 只记「目标实际掉的血」，
/// 溢出另记 <c>OverkillDamage</c>，两者之和才是打出去且没被挡住的量）。
/// ⇒ **溢出算入**（打 20 收 14 血的敌人 → 传参 20），**被格挡的那段仍不计入**；
/// 完全被格挡（没穿透）时不产生传参。
/// 同口径：「攻守兼备」（转格挡）、「目标回馈」（转迭代计数）。
///
/// 打出顺序是「响应 → 本牌效果 → 传参 → 迭代」，所以在 <see cref="PlayEffect"/> 里先取攻击结果快照，
/// 到 <see cref="BuildParam"/> 时直接用它，避免 <c>_resolving</c> 已经还原、取到预览值。
///
/// ★ 已知边界：「灵魂记忆」这类**触发**（不走 <see cref="PlayEffect"/>）读到的仍是上一次打出时的快照，
/// 这与改造前（原来是上一次的卡面值）同口径，未额外处理。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class BattlePreset : AlphaCard
{
    public override bool IsParamSource => true;

    /// <summary>本牌本次造成的**未被格挡伤害**（含溢出），供传参取用。</summary>
    private decimal _dealtDamage;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new AlphaDamageVar(14m, ValueProp.Move),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("battle_preset");

    public override List<(string, string)>? Localization => new CardLoc(
        "战斗预设",
        "造成{Damage:diff()}点伤害。\n[gold]传参[/gold]：响应牌伤害增加本牌造成的未被格挡伤害。",
        ("flavor", "预设好路径，下一次出手只是执行。"));

    public BattlePreset()
        : base(2, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        AttackCommand attack = await DamageCmd.Attack(RawDamage())
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 传参 = 「未被格挡的伤害」（打出去的总伤害 − 被格挡那段），**溢出算入**。
        // 目标有格挡时只算穿透的那部分；被完全格挡 = 0 → 见 BuildParam（不入池）。
        List<DamageResult> results = attack.Results.SelectMany(hit => hit).ToList();
        int total = results.Sum(result => result.TotalDamage);         // 打出去的总伤害（含被格挡段）
        int blocked = results.Sum(result => result.BlockedDamage);     // 被格挡掉的
        int unblocked = results.Sum(result => result.UnblockedDamage); // 目标实际掉血（引擎已不含溢出）
        int overkill = results.Sum(result => result.OverkillDamage);   // 溢出（击杀时超出剩余血量的部分）
        _dealtDamage = total - blocked;                                // = unblocked + overkill

        // 诊断：池里显示的数与「打出去多少」对不上时，看这一行就知道差值来自格挡还是溢出。
        Log.Info($"[MyChar] 战斗预设：本牌伤害 {total}（被格挡 {blocked}）、实际掉血 {unblocked}、" +
                 $"溢出 {overkill} → 传参 +{_dealtDamage:0}");
    }

    protected override ParamEntry? BuildParam() =>
        // 完全被格挡（没造成未被格挡伤害）就不产生传参：没有掉血就没有参数可传。
        _dealtDamage <= 0m
            ? null
            : new()
            {
                Kind = ParamKind.DamageAdd,
                Value = _dealtDamage,
                SourceId = "BATTLE_PRESET",
            };

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(4m);
}
