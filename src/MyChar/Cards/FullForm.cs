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
/// 先古能力（尘封魔典开出的完全形态）：虚无（升级去除，词条由 BaseKeywords 提供，描述里不再重复写「虚无」）；
/// 迭代牌完成迭代时，所有迭代牌计数 +1（每回合一次）；
/// 响应牌触发响应时，返回本次消耗的一个随机传参。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class FullForm : AlphaCard
{
    protected override IEnumerable<CardKeyword> BaseKeywords => new[] { CardKeyword.Ethereal };

    public override string? CustomPortraitPath => MyCharAssets.Card("full_form");

    public override List<(string, string)>? Localization => new CardLoc(
        "完全形态",
        "迭代牌完成[gold]迭代[/gold]时，所有迭代牌计数+1（每回合一次）。\n响应牌触发[gold]响应[/gold]时，返回本次消耗的一个随机[gold]传参[/gold]。",
        ("flavor", "当所有回路同时闭合，它就成了完整的自己。"));

    public FullForm()
        : base(3, CardType.Power, CardRarity.Ancient, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<FullFormPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Ethereal);
    }
}