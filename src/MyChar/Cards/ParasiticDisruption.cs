using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 稀有技能「寄生扰乱」：给予 5（7）层[gold]中毒[/gold]；
/// [gold]传参[/gold]：响应牌的攻击目标获得 5（7）层[gold]中毒[/gold]；
/// [gold]迭代1[/gold]：本牌与传参的中毒层数 +1（2）。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class ParasiticDisruption : AlphaCard
{
    public override bool IsParamSource => true;

    public override int IterationEvery => 1;

    public override IReadOnlyDictionary<string, decimal> IterationVarBonus =>
        new Dictionary<string, decimal>
        {
            ["Poison"] = DynamicVars.TryGetValue("IterGain", out DynamicVar? gain) ? gain.BaseValue : 1m,
        };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Poison", 5m),
        new DynamicVar("IterGain", 1m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("infiltration");

    public override List<(string, string)>? Localization => new CardLoc(
        "寄生扰乱",
        "给予{Poison:diff()}层[gold]中毒[/gold]。\n[gold]传参[/gold]：响应牌的攻击目标获得{Poison:diff()}层[gold]中毒[/gold]。\n[gold]迭代{IterEvery}[/gold]（已打出{IterCount}次）：本牌与传参的中毒层数+{IterGain:diff()}。",
        ("flavor", "让它先住进来，再决定什么时候发作。"));

    public ParasiticDisruption()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    /// <summary>含迭代成长的中毒层数（不含传参增幅，供直伤与传参共用）。</summary>
    private decimal PoisonValue =>
        DynamicVars["Poison"].BaseValue + IterationBonusFor("Poison");

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await PowerCmd.Apply<PoisonPower>(choiceContext, cardPlay.Target,
            ResolveValue("Poison", IterationBonusFor("Poison")), Owner.Creature, this);
    }

    protected override ParamEntry? BuildParam() =>
        new()
        {
            Kind = ParamKind.Debuff,
            Value = PoisonValue,
            Variant = (int)DebuffKind.Poison,
            SourceId = "PARASITIC_DISRUPTION",
        };

    protected override void OnUpgrade()
    {
        DynamicVars["Poison"].UpgradeValueBy(2m);
        DynamicVars["IterGain"].UpgradeValueBy(1m);
    }
}