using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 起始攻击牌：纯响应牌 + 迭代成长。
///
/// 打出时消费传参池；池空也能打出（只是没有加成），靠「参数激活」攒池收益更高。
/// 每打出一次，本场战斗内本牌伤害永久 +1，从下一次打出起生效。
/// 实现 <see cref="ITranscendenceCard"/>：被「古老牙齿」转化为阿尔法锚点（A3）。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class AlphaStrike : AlphaCard, ITranscendenceCard
{
    public override bool IsResponse => true;

    public override int IterationEvery => 1;

    /// <summary>迭代成长：每打出一次 +1（升级后 +2），走 IterGain 变量以便卡面显示正确数值。</summary>
    public override IReadOnlyDictionary<string, decimal> IterationVarBonus =>
        new Dictionary<string, decimal> { ["Damage"] = DynamicVars["IterGain"].BaseValue };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new AlphaDamageVar(3m, ValueProp.Move),
        new DynamicVar("IterGain", 1m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("alpha_strike");

    public override List<(string, string)>? Localization => new CardLoc(
        "阿尔法突袭",
        "随机造成{Damage:diff()}点伤害3次。\n[gold]迭代1[/gold]（已打出{IterCount}次）：伤害+{IterGain:diff()}。",
        ("flavor", "每一次应答，都比上一次更重。"));

    public AlphaStrike()
        : base(1, CardType.Attack, CardRarity.Basic, TargetType.RandomEnemy)
    {
    }

    /// <summary>「古老牙齿」转化：阿尔法突袭 → 阿尔法锚点。</summary>
    public CardModel GetTranscendenceTransformedCard() => ModelDb.Card<AlphaAnchor>();

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ICombatState? combatState = CombatState ?? Owner.Creature.CombatState;

        await DamageCmd.Attack(RawDamage())
            .FromCard(this, cardPlay)
            .TargetingRandomOpponents(combatState!, true)
            .WithHitCount(3)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["IterGain"].UpgradeValueBy(1m);
    }
}