using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 普通运转技能：抽 X 张牌（最多 5 张），X = 当前[gold]传参池[/gold]条数。
///
/// 升级：费用 1 → 0，并且 **X +1**；**上限仍是 5 张，不随升级变大**——
/// 先算完 X（含升级的 +1）再夹上限，所以「池子够满」时升级不额外多抽（都是 5）。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class Assessment : AlphaCard
{
    /// <summary>抽牌上限。升级不改变它。</summary>
    private const int DrawCap = 5;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Cap", DrawCap),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("assessment");

    public override List<(string, string)>? Localization => new CardLoc(
        "评估",
        "抽 X{IfUpgraded:show:+1|} 张牌（最多{Cap}张），X 为当前[gold]传参池[/gold]中的传参数。",
        ("flavor", "先数一遍池子里有什么，再决定怎么用。"));

    public Assessment()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int x = AlphaCorePower.Of(this)?.ParamCount ?? 0;
        if (IsUpgraded)
        {
            x += 1;
        }
        x = Math.Min(x, DrawCap);
        if (x > 0)
        {
            await CardPileCmd.Draw(choiceContext, x, Owner);
        }
    }

    /// <summary>升级只降费（1 → 0）。多抽的 1 张由卡面描述里的 X+1 体现，上限不变。</summary>
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}
