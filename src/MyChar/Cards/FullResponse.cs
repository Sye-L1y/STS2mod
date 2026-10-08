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

/// <summary>
/// 完全响应：本次战斗里，凡是它响应吃到的传参都会永久挂在这张牌上，
/// 之后每次打出都先重放一遍（哪怕池里已经空了）。
/// 已经挂上的传参会直接列在卡面描述里（见 AlphaCard.RetainedText）。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class FullResponse : AlphaCard
{
    public override bool IsResponse => true;

    public override bool RetainsResponse => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new AlphaDamageVar(1m, ValueProp.Move),
        new AlphaBlockVar(1m, ValueProp.Move),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("full_response");

    public override List<(string, string)>? Localization => new CardLoc(
        "完全响应",
        "对所有敌人造成{Damage:diff()}点伤害，获得{Block:diff()}点[gold]格挡[/gold]。\n本场战斗中，本牌吃到的[gold]传参[/gold]会保留在本牌上。{Retained}",
        ("flavor", "不只是一次应答——它把答案记了下来。"));

    public FullResponse()
        : base(0, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies)
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
        await CreatureCmd.GainBlock(Owner.Creature, RawBlock(), ValueProp.Move, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.Damage.UpgradeValueBy(1m);
        DynamicVars.Block.UpgradeValueBy(1m);
        // 保留走 AddKeyword：BaseKeywords 依赖 IsUpgraded 时，LocalKeywords 会在升级前就被缓存，升级后不刷新。
        AddKeyword(CardKeyword.Retain);
    }
}