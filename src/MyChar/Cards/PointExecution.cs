using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 普通骨架攻击：本回合第一张攻击牌才吃额外伤害，逼玩家把它排在回合最前面。
/// 判定排除本牌自身——打出瞬间它可能已经进了历史。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class PointExecution : AlphaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new AlphaDamageVar(9m, ValueProp.Move),
        new DamageVar("Bonus", 6m, ValueProp.Move),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("point_execution");

    public override List<(string, string)>? Localization => new CardLoc(
        "定点执行",
        "造成{Damage:diff()}点伤害。若本回合尚未打出过攻击牌，额外造成{Bonus:diff()}点伤害。",
        ("flavor", "第一发最准。"));

    public PointExecution()
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

        if (IsFirstAttackThisTurn())
        {
            // 额外伤害同样是本牌的伤害：走 ResolveDamageValue，传参池的加伤（+X% / +N）对它一样生效。
            await DamageCmd.Attack(ResolveDamageValue("Bonus"))
                .FromCard(this, cardPlay)
                .Targeting(cardPlay.Target)
                .WithHitFx("vfx/vfx_attack_slash")
                .Execute(choiceContext);
        }
    }

    private bool IsFirstAttackThisTurn() =>
        !CombatManager.Instance.History.CardPlaysFinished.Any(e =>
            e.HappenedThisTurn(CombatState) &&
            e.CardPlay.Card.Type == CardType.Attack &&
            e.CardPlay.Player == Owner &&
            e.CardPlay.Card != this);

    protected override void OnUpgrade()
    {
        DynamicVars["Bonus"].UpgradeValueBy(3m);
    }
}