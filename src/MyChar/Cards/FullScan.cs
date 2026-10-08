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

/// <summary>普通骨架攻击：全体伤害，并把协议层数留到下回合结算（跨回合攒档）。</summary>
[Pool(typeof(MyCharCardPool))]
public sealed class FullScan : AlphaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new AlphaDamageVar(7m, ValueProp.Move),
        new DynamicVar("Protocol", 3m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("full_scan");

    public override List<(string, string)>? Localization => new CardLoc(
        "全量扫描",
        "对所有敌人造成{Damage:diff()}点伤害。下回合获得{Protocol:diff()}层[gold]协议[/gold]。",
        ("flavor", "扫描整片战场，顺手把结果写进协议。"));

    public FullScan()
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

        await PowerCmd.Apply<PendingProtocolPower>(choiceContext, Owner.Creature,
            DynamicVars["Protocol"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Protocol"].UpgradeValueBy(1m);
    }
}