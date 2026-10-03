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
/// 普通技能：把传参池当成燃料——每有一条传参换 1 点能量，代价是生命。
/// 按 P5 不加自伤保护：池子攒过头真的会把自己烧死。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class OverloadState : AlphaCard
{
    protected override IEnumerable<CardKeyword> BaseKeywords => new[] { CardKeyword.Exhaust };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new HpLossVar(2m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("overload_state");

    public override List<(string, string)>? Localization => new CardLoc(
        "过载状态",
        "每有一条[gold]传参[/gold]：获得1点[gold]能量[/gold]，失去{HpLoss:diff()}点生命。",
        ("flavor", "把整池参数一次烧掉，换这一回合的爆发。"));

    public OverloadState()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int count = AlphaCorePower.Of(this)?.ParamCount ?? 0;
        if (count <= 0)
        {
            return;
        }

        await PlayerCmd.GainEnergy(count, Owner);
        await CreatureCmd.Damage(choiceContext, Owner.Creature,
            DynamicVars.HpLoss.BaseValue * count,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, this, cardPlay);
    }

    protected override void OnUpgrade()
    {
        DynamicVars.HpLoss.UpgradeValueBy(-1m);
    }
}