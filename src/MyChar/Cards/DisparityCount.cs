using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 普通运转技能：一次抽一大把，只留下带机制联动的牌，其余统一丢掉。
/// 先抽满再统一弃，所以抽到的牌会短暂地在手里出现一下。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class DisparityCount : AlphaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Draw", 4m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("disparity_count");

    public override List<(string, string)>? Localization => new CardLoc(
        "悬殊计数",
        "抽{Draw:diff()}张牌，然后丢弃其中所有既无[gold]迭代[/gold]也无[gold]响应[/gold]的牌。",
        ("flavor", "把没用的数据直接筛掉。"));

    public DisparityCount()
        : base(0, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        IEnumerable<CardModel> drawn =
            await CardPileCmd.Draw(choiceContext, DynamicVars["Draw"].BaseValue, Owner);

        List<CardModel> trash = drawn.Where(IsDisparate).ToList();
        if (trash.Count > 0)
        {
            await CardCmd.Discard(choiceContext, trash);
        }
    }

    /// <summary>既不带迭代也不带响应 = 和核心机制没有联动。</summary>
    private static bool IsDisparate(CardModel card) =>
        card is not AlphaCard alpha || (!alpha.IsResponse && alpha.IterationEvery <= 0);

    protected override void OnUpgrade()
    {
        DynamicVars["Draw"].UpgradeValueBy(1m);
    }
}