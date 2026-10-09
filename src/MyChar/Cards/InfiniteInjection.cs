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
/// 罕见能力「无限注入」：（固有）每回合开始时，[gold]传参[/gold]：获得 1 层[gold]协议[/gold]。
/// 由 <see cref="InfiniteInjectionPower"/> 承载（走 <c>AddParamWithHook</c> 入池，E18 监听者照常触发）。2 费。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class InfiniteInjection : AlphaCard
{
    protected override IEnumerable<CardKeyword> BaseKeywords => new[] { CardKeyword.Innate };

    public override string? CustomPortraitPath => MyCharAssets.Card("infinite_injection");

    public override List<(string, string)>? Localization => new CardLoc(
        "无限注入",
        "每回合开始时，[gold]传参[/gold]：获得1层[gold]协议[/gold]。",
        ("flavor", "只要接口不关，数据就会一直流进来。"));

    public InfiniteInjection()
        : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<InfiniteInjectionPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }
}
