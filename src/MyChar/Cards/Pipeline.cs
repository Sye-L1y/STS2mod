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
/// 罕见能力「流水线」：（固有）每回合开始时额外抽 1 张牌，同时手牌上限 −1。
/// 由 <see cref="PipelinePower"/> 承载（含 <c>IMaxHandSizeModifier</c> 手牌上限钩子）。0 费。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class Pipeline : AlphaCard
{
    protected override IEnumerable<CardKeyword> BaseKeywords => new[] { CardKeyword.Innate };

    public override string? CustomPortraitPath => MyCharAssets.Card("pipeline");

    public override List<(string, string)>? Localization => new CardLoc(
        "流水线",
        "每回合开始时额外抽1张牌；手牌上限-1。",
        ("flavor", "把动作拆成工位，产出就会稳定。"));

    public Pipeline()
        : base(0, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<PipelinePower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }
}
