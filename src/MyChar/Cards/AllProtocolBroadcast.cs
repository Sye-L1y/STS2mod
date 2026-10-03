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

/// <summary>稀有高费攻击：全体大伤害，顺带一次灌 4 层协议。</summary>
[Pool(typeof(MyCharCardPool))]
public sealed class AllProtocolBroadcast : AlphaCard
{
    public override bool GrantsProtocol => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new AlphaDamageVar(16m, ValueProp.Move),
        new DynamicVar("Protocol", 4m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("all_protocol_broadcast");

    public override List<(string, string)>? Localization => new CardLoc(
        "全协议广播",
        "对所有敌人造成{Damage:diff()}点伤害。获得{Protocol:diff()}层[gold]协议[/gold]。",
        ("flavor", "一次广播，覆盖所有终端。"));

    public AllProtocolBroadcast()
        : base(3, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
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

        ProtocolPower.Of(this)?.AddProtocol((int)DynamicVars["Protocol"].BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(6m);
        DynamicVars["Protocol"].UpgradeValueBy(2m);
    }
}