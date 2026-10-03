using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 罕见攻击「龙吼模块」：获得 2（3）点临时力量；随机造成 4 点伤害两次；
/// 消耗 3 层协议时，先给全体敌人 1（2）层易伤一次。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class ProtocolExtract : AlphaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new AlphaDamageVar(4m, ValueProp.Move),
        new DynamicVar("TempStrength", 2m),
        new DynamicVar("Vulnerable", 1m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("protocol_extract");

    public override List<(string, string)>? Localization => new CardLoc(
        "龙吼模块",
        "获得{TempStrength:diff()}点[gold]临时力量[/gold]。随机造成{Damage:diff()}点伤害两次。\n消耗3层[gold]协议[/gold]：先给予全体敌人{Vulnerable:diff()}层[gold]易伤[/gold]。",
        ("flavor", "把授权直接榨成一声咆哮。"));

    public ProtocolExtract()
        : base(1, CardType.Attack, CardRarity.Uncommon, TargetType.AnyEnemy)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ICombatState? combatState = CombatState ?? Owner.Creature.CombatState;
        if (combatState == null)
        {
            return;
        }

        await PowerCmd.Apply<DragonRoarTempStrengthPower>(choiceContext, Owner.Creature,
            ResolveValue("TempStrength"), Owner.Creature, this);

        // 「先」给易伤：在伤害之前结算。协议不足 3 层时不消耗、不触发（避免白白掉层）。
        if (CurrentProtocol >= 3)
        {
            ConsumeProtocol(3);
            foreach (Creature enemy in combatState.HittableEnemies)
            {
                await PowerCmd.Apply<VulnerablePower>(choiceContext, enemy,
                    ResolveValue("Vulnerable"), Owner.Creature, this);
            }
        }

        await DamageCmd.Attack(RawDamage())
            .FromCard(this, cardPlay)
            .TargetingRandomOpponents(combatState, true)
            .WithHitCount(2)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["TempStrength"].UpgradeValueBy(1m);
        DynamicVars["Vulnerable"].UpgradeValueBy(1m);
    }
}