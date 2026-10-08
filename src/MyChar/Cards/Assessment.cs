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

/// <summary>普通运转技能：按传参池条数抽牌，池越满抽得越多（上限 5 张，升级 6 张）。</summary>
[Pool(typeof(MyCharCardPool))]
public sealed class Assessment : AlphaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        // 抽牌上限做成动态变量：升级 +1 会以绿字 ({Cap:diff()}) 反映在卡面上。
        new DynamicVar("Cap", 5m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("assessment");

    public override List<(string, string)>? Localization => new CardLoc(
        "评估",
        "抽 X 张牌（最多{Cap:diff()}张），X 为当前[gold]传参池[/gold]中的传参数。",
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
        x = Math.Min(x, DynamicVars["Cap"].IntValue);
        if (x > 0)
        {
            await CardPileCmd.Draw(choiceContext, x, Owner);
        }
    }

    /// <summary>升级同时降费（1 → 0）与多抽 1 张（上限 5 → 6）。</summary>
    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars["Cap"].UpgradeValueBy(1m);
    }
}
