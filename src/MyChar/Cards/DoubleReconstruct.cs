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
using MegaCrit.Sts2.Core.ValueProps;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 稀有攻击「双重验证」：造成 11（14）伤害；
/// [gold]迭代2[/gold]：伤害 +8（11），且先给予一次一层易伤（叠加几次就分开施加几次）。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class DoubleReconstruct : AlphaCard
{
    public override int IterationEvery => 2;

    public override IReadOnlyDictionary<string, decimal> IterationVarBonus =>
        new Dictionary<string, decimal>
        {
            ["Damage"] = DynamicVars.TryGetValue("IterGain", out DynamicVar? gain) ? gain.BaseValue : 8m,
        };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new AlphaDamageVar(11m, ValueProp.Move),
        new DynamicVar("IterGain", 8m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("double_reconstruct");

    public override List<(string, string)>? Localization => new CardLoc(
        "双重验证",
        "造成{Damage:diff()}点伤害。\n[gold]迭代2[/gold]（已打出{IterCount}次）：伤害+{IterGain:diff()}，且先给予一次一层[gold]易伤[/gold]。",
        ("flavor", "每验证一次，结构就更硬一分。"));

    public DoubleReconstruct()
        : base(2, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        // 「给予一次一层易伤」按次数分开施加：叠加 X 次 → 施加 X 次、每次 1 层。
        for (int i = 0; i < IterationStacks; i++)
        {
            await PowerCmd.Apply<VulnerablePower>(choiceContext, cardPlay.Target, 1m, Owner.Creature, this);
        }

        await DamageCmd.Attack(RawDamage())
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars["IterGain"].UpgradeValueBy(3m);
    }
}