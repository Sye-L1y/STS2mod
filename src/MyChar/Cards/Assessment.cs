using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>普通运转技能：按传参池条数抽牌，池越满抽得越多（上限 5 张）。</summary>
[Pool(typeof(MyCharCardPool))]
public sealed class Assessment : AlphaCard
{
    /// <summary>抽牌上限：池再满也只抽这么多，避免一次性抽空牌库。</summary>
    private const int DrawCap = 5;

    public override string? CustomPortraitPath => MyCharAssets.Card("assessment");

    public override List<(string, string)>? Localization => new CardLoc(
        "评估",
        "抽 X 张牌（最多 5 张），X 为当前[gold]传参池[/gold]中的传参数。",
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

    /// <summary>升级同时降费（1 → 0）与多抽 1 张。</summary>
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}