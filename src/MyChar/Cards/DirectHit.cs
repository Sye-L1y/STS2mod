using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>普通迭代攻击：全体伤害，每次打出后伤害再涨一档（每命中一个敌人都吃这份加成）。</summary>
[Pool(typeof(MyCharCardPool))]
public sealed class DirectHit : AlphaCard
{
    public override int IterationEvery => 1;

    public override IReadOnlyDictionary<string, decimal> IterationVarBonus =>
        new Dictionary<string, decimal>
        {
            ["Damage"] = DynamicVars.TryGetValue("IterGain", out DynamicVar? gain) ? gain.BaseValue : 1m,
        };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new AlphaDamageVar(7m, ValueProp.Move),
        new DynamicVar("IterGain", 1m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("direct_hit");

    public override List<(string, string)>? Localization => new CardLoc(
        "正击",
        "对所有敌人造成{Damage:diff()}点伤害。\n[gold]迭代{IterEvery}[/gold]（已打出{IterCount}次）：每命中一个敌人伤害+{IterGain}。",
        ("flavor", "一次应答，覆盖全部目标。"));

    public DirectHit()
        : base(1, CardType.Attack, CardRarity.Common, TargetType.AllEnemies)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ICombatState? combatState = CombatState ?? Owner.Creature.CombatState;
        await DamageCmd.Attack(RawDamage())
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(combatState!)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars["IterGain"].UpgradeValueBy(1m);
    }
}