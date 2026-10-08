using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 先古攻击（阿尔法突袭经「古老牙齿」转化而来）：全体 4 伤 ×N，响应；
/// 迭代1：每打出一次，本局游戏内本牌伤害次数永久 +1（跨战斗，随牌存档）。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class AlphaAnchor : AlphaCard
{
    private const decimal BaseHits = 2m;

    private int _bonusHits;

    public override bool IsResponse => true;

    public override int IterationEvery => 1;

    /// <summary>本局游戏内累计多打的次数（跨战斗持久化，见 <c>GeneticAlgorithm</c> 的写法）。</summary>
    [SavedProperty]
    public int BonusHits
    {
        get => _bonusHits;
        set
        {
            AssertMutable();
            _bonusHits = value;
            DynamicVars["Hits"].BaseValue = BaseHits + _bonusHits;
        }
    }

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new AlphaDamageVar(4m, ValueProp.Move),
        new DynamicVar("Hits", BaseHits),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("alpha_anchor");

    public override List<(string, string)>? Localization => new CardLoc(
        "阿尔法锚点",
        "对所有敌人造成{Damage:diff()}点伤害{Hits}次。\n[gold]迭代1[/gold]：每打出一次，本局游戏内本牌伤害次数+1。",
        ("flavor", "钉在时间线上的锚，越用越沉。"));

    public AlphaAnchor()
        : base(1, CardType.Attack, CardRarity.Ancient, TargetType.AllEnemies)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ICombatState? combatState = CombatState ?? Owner.Creature.CombatState;
        if (combatState == null)
        {
            return;
        }

        int hits = Math.Max(1, (int)DynamicVars["Hits"].BaseValue);
        await DamageCmd.Attack(RawDamage())
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(combatState)
            .WithHitCount(hits)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 本场战斗的临时副本 + 牌组里的持久副本一起涨，跨战斗才留得住。
        BonusHits += 1;
        (DeckVersion as AlphaAnchor)?.BuffFromPlay();
    }

    protected override void AfterDowngraded()
    {
        DynamicVars["Hits"].BaseValue = BaseHits + _bonusHits;
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }

    private void BuffFromPlay() => BonusHits += 1;
}