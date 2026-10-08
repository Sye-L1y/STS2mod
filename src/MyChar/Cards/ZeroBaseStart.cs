using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 稀有攻击「零基启动」：0 费。造成 4 点伤害；别处触发[gold]响应[/gold]时从抽牌堆 / 弃牌堆回到手牌
/// （消耗堆里的不捞）。[gold]迭代3[/gold]：每完成一次迭代，伤害 += 1（2）× 完成时的本场已打出牌数
/// （累计快照，不是实时值）。自身**不带「响应」**。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class ZeroBaseStart : AlphaCard, IAlphaResponseReactor
{
    public override int IterationEvery => 3;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new AlphaDamageVar(4m, ValueProp.Move),
        new DynamicVar("PerPlay", 1m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("zero_base_start");

    public override List<(string, string)>? Localization => new CardLoc(
        "零基启动",
        "造成{Damage:diff()}点伤害。触发[gold]响应[/gold]时回到手牌。\n[gold]迭代3[/gold]（已打出{IterCount}次）：伤害+{PerPlay:diff()}×本场战斗已打出的牌数。",
        ("flavor", "从零开始，但每转一圈都更硬一分。"));

    public ZeroBaseStart()
        : base(0, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    /// <summary>迭代成长按「完成时机快照」累计，不用实时的已打牌数，所以走 AlphaCorePower 的动态加成。</summary>
    protected override decimal IterationBonusFor(string varName) =>
        varName == "Damage" ? AlphaCorePower.Of(this)?.GetDynamicIterationBonus(this) ?? 0m : 0m;

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        await DamageCmd.Attack(RawDamage())
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override Task OnIterationCompleted(PlayerChoiceContext choiceContext)
    {
        AlphaCorePower? core = AlphaCorePower.Of(this);
        if (core != null)
        {
            core.AddDynamicIterationBonus(this, DynamicVars["PerPlay"].BaseValue * core.TotalPlays);
        }
        return Task.CompletedTask;
    }

    /// <summary>别处触发响应时回手：只从抽牌堆 / 弃牌堆捞，消耗堆里的不动。</summary>
    public async Task OnResponseTriggered(PlayerChoiceContext choiceContext, Player player)
    {
        if (Owner == player && Pile?.Type is PileType.Draw or PileType.Discard)
        {
            await CardPileCmd.Add(this, PileType.Hand);
        }
    }

    protected override void OnUpgrade() => DynamicVars["PerPlay"].UpgradeValueBy(1m);
}