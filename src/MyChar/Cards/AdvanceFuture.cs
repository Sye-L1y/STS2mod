using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 罕见技能「预支未来」：消耗；从 3 张本职业随机牌中选 1 张加入手牌，本回合可免费打出；
/// [gold]传参[/gold]：从 3 张本职业随机牌中选 1 张加入手牌。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class AdvanceFuture : AlphaCard
{
    public override bool IsParamSource => true;

    protected override IEnumerable<CardKeyword> BaseKeywords => new[] { CardKeyword.Exhaust };

    public override string? CustomPortraitPath => MyCharAssets.Card("advance_future");

    public override List<(string, string)>? Localization => new CardLoc(
        "预支未来",
        "从3张本职业随机牌中选择1张加入手牌，本回合可以免费打出。\n[gold]传参[/gold]：从3张本职业随机牌中选择1张加入手牌。",
        ("flavor", "把明天的牌今天就拿来用。"),
        ("selectionScreenPrompt", "选择 1 张牌加入手牌"));

    public AdvanceFuture()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await GenerateClassCardChoice(choiceContext, freeThisTurn: true);
    }

    protected override ParamEntry? BuildParam() =>
        new()
        {
            Kind = ParamKind.ChooseClassCard,
            Value = 1m,
            SourceId = "ADVANCE_FUTURE",
        };
}