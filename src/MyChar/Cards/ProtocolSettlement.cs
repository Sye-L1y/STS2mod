using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 稀有攻击「歼灭龙息」：消耗全部协议，每层对随机敌人造成 7（10）伤；
/// 若消耗了 5 层以上，则回补 5 层协议。
/// 0 层时无伤害，但仍可打出（不加门禁）。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class ProtocolSettlement : AlphaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("PerLayer", 7m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("protocol_settlement");

    public override List<(string, string)>? Localization => new CardLoc(
        "歼灭龙息",
        "消耗全部[gold]协议[/gold]：每层对随机敌人造成{PerLayer:diff()}点伤害。若消耗了5层以上，则获得5层[gold]协议[/gold]。",
        ("flavor", "把攒下的授权一次性清账。"));

    public ProtocolSettlement()
        : base(2, CardType.Attack, CardRarity.Rare, TargetType.RandomEnemy)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ICombatState? combatState = CombatState ?? Owner.Creature.CombatState;
        if (combatState == null)
        {
            return;
        }

        ProtocolPower? protocol = ProtocolPower.Of(this);
        int layers = protocol == null ? 0 : await protocol.ConsumeAllWithBroadcast(choiceContext);
        if (layers <= 0)
        {
            return;
        }

        // 表 §4：消耗了 5 层以上，则协议 5（回补 5 层）。
        if (layers >= 5)
        {
            ProtocolPower.Of(this)?.AddProtocol(5);
        }

        // ★ 用 ResolveDamageValue 而不是 ResolveValue：被「语义附加」授予响应后，
        //   传参池里的「伤害 +X% / +N」必须加在每层伤害上（ResolveValue 只吃 α 协议的增幅）。
        await DamageCmd.Attack(ResolveDamageValue("PerLayer"))
            .FromCard(this, cardPlay)
            .TargetingRandomOpponents(combatState, true)
            .WithHitCount(layers)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["PerLayer"].UpgradeValueBy(3m);
    }
}