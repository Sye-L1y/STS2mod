using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 先古能力（尘封魔典开出的完全形态）：虚无（升级去除，词条由 BaseKeywords 提供，描述里不再重复写「虚无」）；
/// 迭代牌完成迭代时，所有迭代牌计数 +1（每回合一次，每多打一份再多 +1）；
/// 响应牌触发响应时，随机返还本次消耗的传参（每份 1 条，每次独立随机、可重复）。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class FullForm : AlphaCard
{
    protected override IEnumerable<CardKeyword> BaseKeywords => new[] { CardKeyword.Ethereal };

    public override string? CustomPortraitPath => MyCharAssets.Card("full_form");

    public override List<(string, string)>? Localization => new CardLoc(
        "完全形态",
        "迭代牌完成[gold]迭代[/gold]时，所有迭代牌计数+1（每回合一次）。\n响应牌触发[gold]响应[/gold]时，随机返还本次消耗的[gold]传参[/gold]。",
        ("flavor", "当所有回路同时闭合，它就成了完整的自己。"));

    public FullForm()
        : base(3, CardType.Power, CardRarity.Ancient, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<FullFormPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }

    /// <summary>
    /// 诊断用：这张牌每次进战斗时把「实际词条 + 升级档」写一行日志。
    /// 只是回答「完全形态是不是固有」——词条里没有 Innate 就说明它没带固有，开局上手是抽牌随机（种子）。
    /// 若确认没问题，这段可以整块删掉。
    /// </summary>
    public override Task AfterCardEnteredCombat(CardModel card)
    {
        if (ReferenceEquals(card, this))
        {
            Log.Info($"[MyChar] 完全形态入场：升级 {CurrentUpgradeLevel}，词条 " +
                     $"[{string.Join("、", Keywords)}]");
        }
        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        RemoveKeyword(CardKeyword.Ethereal);
    }
}
