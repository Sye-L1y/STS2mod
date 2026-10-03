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

/// <summary>普通响应牌：全体伤害。清池时能一次覆盖所有敌人。</summary>
[Pool(typeof(MyCharCardPool))]
public sealed class DirectHit : AlphaCard
{
    public override bool IsResponse => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new AlphaDamageVar(7m, ValueProp.Move),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("direct_hit");

    public override List<(string, string)>? Localization => new CardLoc(
        "正击",
        "对所有敌人造成{Damage:diff()}点伤害。",
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
    }
}