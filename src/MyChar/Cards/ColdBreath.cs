using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 罕见能力「冷息」：（[gold]固有[/gold]）每回合结束时，手牌中的[gold]迭代[/gold]牌获得[gold]保留[/gold]。
/// 由 <see cref="ColdBreathPower"/> 承载（<c>BeforeSideTurnEnd</c> 打单回合保留标记）。
///
/// 「固有」由 <see cref="BaseKeywords"/> 提供，卡面词条自动生成，描述里不重复写。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class ColdBreath : AlphaCard
{
    protected override IEnumerable<CardKeyword> BaseKeywords => new[] { CardKeyword.Innate };

    public override string? CustomPortraitPath => MyCharAssets.Card("cold_breath");

    public override List<(string, string)>? Localization => new CardLoc(
        "冷息",
        "每回合结束时，手牌中的[gold]迭代[/gold]牌获得[gold]保留[/gold]。",
        ("flavor", "慢下来，让没跑完的循环再多活一个回合。"));

    public ColdBreath()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<ColdBreathPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }
}
