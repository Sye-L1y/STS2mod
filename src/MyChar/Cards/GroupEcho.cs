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

/// <summary>罕见响应攻击「群响」：对所有敌人造成 10（15）伤。</summary>
[Pool(typeof(MyCharCardPool))]
public sealed class GroupEcho : AlphaCard
{
    public override bool IsResponse => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new AlphaDamageVar(10m, ValueProp.Move),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("group_echo");

    public override List<(string, string)>? Localization => new CardLoc(
        "群响",
        "对所有敌人造成{Damage:diff()}点伤害。",
        ("flavor", "一次触发，整片阵列同时回响。"));

    public GroupEcho()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AllEnemies)
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

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(5m);
}