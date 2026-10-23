using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 稀有攻击「目标回馈」：造成 1（2）点伤害；使手牌的一张[gold]迭代[/gold]牌计数器
/// 增加「造成的**未被格挡伤害数**」（含溢出；被格挡掉的那段不算）。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class TargetFeedback : AlphaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new AlphaDamageVar(1m, ValueProp.Move),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("target_feedback");

    public override List<(string, string)>? Localization => new CardLoc(
        "目标回馈",
        "造成{Damage:diff()}点伤害。使手牌的一张[gold]迭代[/gold]牌计数器增加「造成的未被格挡伤害数」。",
        ("flavor", "打出去多少，就回填多少。"),
        ("selectionScreenPrompt", "选择 1 张迭代牌"));

    public TargetFeedback()
        : base(1, CardType.Attack, CardRarity.Rare, TargetType.AnyEnemy)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");

        AttackCommand attack = await DamageCmd.Attack(RawDamage())
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);

        // 未被格挡伤害（含溢出）：打 20 收 14 血的敌人 → 14 + 6 = 20；被格挡段不计入。
        int dealt = attack.Results.SelectMany(hit => hit)
            .Sum(result => result.TotalDamage - result.BlockedDamage);
        if (dealt <= 0)
        {
            return;
        }

        AlphaCorePower? core = AlphaCorePower.Of(this);
        if (core == null)
        {
            return;
        }

        // 手牌里没有迭代牌就直接跳过，不弹选择界面。
        if (!PileType.Hand.GetPile(Owner).Cards.Any(IsIterationCard))
        {
            return;
        }

        IEnumerable<CardModel> picked = await CardSelectCmd.FromHand(choiceContext, Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1), IsIterationCard, this);

        CardModel? chosen = picked.FirstOrDefault();
        if (chosen == null)
        {
            return;
        }

        // 推进迭代计数（并补结算跨过的档位）。
        await core.AdvanceIteration(choiceContext, chosen, dealt);
    }

    private static bool IsIterationCard(CardModel card) =>
        card is AlphaCard { IterationEvery: > 0 };

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(1m);
}