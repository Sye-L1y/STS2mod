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
/// 罕见能力「冷息」：每回合结束时，手牌中的[gold]迭代[/gold]牌获得[gold]保留[/gold]。
/// 由 <see cref="ColdBreathPower"/> 承载（<c>BeforeSideTurnEnd</c> 打单回合保留标记）。
///
/// 「固有」是**升级后**才有的（<see cref="OnUpgrade"/> 里 <c>AddKeyword</c>）；
/// 挂上关键词后卡面词条由引擎自动生成，描述里不重复写。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class ColdBreath : AlphaCard
{
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

    /// <summary>升级收益：获得[gold]固有[/gold]（第一回合必上手）。</summary>
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);
}
