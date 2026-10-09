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

/// <summary>
/// 罕见技能「复空间定位」：获得 8 格挡；本回合受到攻击时，对所有敌人造成 2（升级 4）伤。
/// 反击由 <see cref="DuospacePositioningPower"/> 承载，回合结束移除。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class DuospacePositioning : AlphaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new AlphaBlockVar(8m, ValueProp.Move),
        new DynamicVar("Retaliate", 2m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("duospace_positioning");

    public override List<(string, string)>? Localization => new CardLoc(
        "复空间定位",
        "获得{Block:diff()}点[gold]格挡[/gold]。本回合受到攻击时，对所有敌人造成{Retaliate:diff()}点伤害。",
        ("flavor", "把自己同时定位在两个空间：挨打的是这里，还手的是那里。"));

    public DuospacePositioning()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.GainBlock(Owner.Creature, RawBlock(), ValueProp.Move, cardPlay);
        await PowerCmd.Apply<DuospacePositioningPower>(choiceContext, Owner.Creature,
            ResolveValue("Retaliate"), Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["Retaliate"].UpgradeValueBy(2m);
}