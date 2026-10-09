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
using MegaCrit.Sts2.Core.Models.Powers;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 罕见技能「异常解析」：消耗手牌中**所有状态牌**；然后随机使一个敌人获得
/// X（升级后 X+1）层[gold]脆弱[/gold]、[gold]虚弱[/gold]、[gold]易伤[/gold]（X = 消耗数）。
///
/// 按 v1.10 口径改写：**直接结算**，不再走传参池。技能牌没有敌方目标，所以随机挑一个可命中敌人。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class AnomalyAnalysis : AlphaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Bonus", 0m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("anomaly_analysis");

    public override List<(string, string)>? Localization => new CardLoc(
        "异常解析",
        "消耗手牌中所有状态牌。随机使一个敌人获得X{IfUpgraded:show:+1|}层[gold]脆弱[/gold]、[gold]虚弱[/gold]、[gold]易伤[/gold]（X = 消耗数）。",
        ("flavor", "把报错信息翻译成对方的麻烦。"));

    public AnomalyAnalysis()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        List<CardModel> statuses = PileType.Hand.GetPile(Owner).Cards
            .Where(card => card.Type == CardType.Status)
            .ToList();
        foreach (CardModel card in statuses)
        {
            await CardCmd.Exhaust(choiceContext, card);
        }

        int layers = statuses.Count + (int)DynamicVars["Bonus"].BaseValue;
        if (layers <= 0 || RandomEnemy() is not { } target)
        {
            return;
        }

        await PowerCmd.Apply<VulnerablePower>(choiceContext, target, layers, Owner.Creature, this);
        await PowerCmd.Apply<WeakPower>(choiceContext, target, layers, Owner.Creature, this);
        await PowerCmd.Apply<FrailPower>(choiceContext, target, layers, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["Bonus"].UpgradeValueBy(1m);
}
