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
/// 普通攻击「二次输入」：造成 7（10）点伤害；若[gold]传参[/gold]池不为空，
/// **重复上一次入池的传参**（等价于重复上一次打出的传参牌所带的传参）。
///
/// 做法：不自己造传参，而是把「最近一次入池批次」原样再抛一遍
/// （<see cref="AlphaCorePower.LastEnteredParams"/>，由 <see cref="AlphaCorePower.BeginParamBatch"/> 分段）。
/// 池空时不产生任何传参（没有「上一次」可重复）。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class SecondInput : AlphaCard
{
    public override bool IsParamSource => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new AlphaDamageVar(7m, ValueProp.Move),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("second_input");

    public override List<(string, string)>? Localization => new CardLoc(
        "二次输入",
        "造成{Damage:diff()}点伤害。若[gold]传参[/gold]池不为空，重复上一次入池的[gold]传参[/gold]。",
        ("flavor", "同一段输入再喂一遍，输出就会翻倍。"));

    public SecondInput()
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

    /// <summary>池不为空时，把「最近一次入池的传参」原样再抛一遍。</summary>
    protected override IEnumerable<ParamEntry> BuildParams()
    {
        AlphaCorePower? core = AlphaCorePower.Of(this);
        if (core == null || !core.HasParams)
        {
            yield break;
        }
        foreach (ParamEntry entry in core.LastEnteredParams)
        {
            yield return entry;
        }
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
