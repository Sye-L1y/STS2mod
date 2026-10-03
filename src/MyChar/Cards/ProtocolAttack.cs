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

/// <summary>普通攻击牌：伤害 + 写协议。比「协议注入」更偏协议（升级同时加伤害与层数）。</summary>
[Pool(typeof(MyCharCardPool))]
public sealed class ProtocolAttack : AlphaCard
{
    public override bool GrantsProtocol => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new AlphaDamageVar(6m, ValueProp.Move),
        new DynamicVar("Protocol", 1m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("protocol_attack");

    public override List<(string, string)>? Localization => new CardLoc(
        "协议攻击",
        "造成{Damage:diff()}点伤害。获得{Protocol:diff()}层[gold]协议[/gold]。",
        ("flavor", "每一击都在协议里留一行记录。"));

    public ProtocolAttack()
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

        ProtocolPower.Of(this)?.AddProtocol((int)DynamicVars["Protocol"].BaseValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(3m);
        DynamicVars["Protocol"].UpgradeValueBy(1m);
    }
}