using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>普通技能：读敌人意图再决定削弱方向——要打人就削它，不打人就趁它露破绽。</summary>
[Pool(typeof(MyCharCardPool))]
public sealed class Foreshadow : AlphaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Amount", 2m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("foreshadow");

    public override List<(string, string)>? Localization => new CardLoc(
        "预演",
        "若目标意图为攻击，给予其{Amount:diff()}层[gold]虚弱[/gold]；否则给予其{Amount:diff()}层[gold]易伤[/gold]。",
        ("flavor", "先看它要做什么，再决定怎么接。"));

    public Foreshadow()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        decimal amount = DynamicVars["Amount"].BaseValue;

        if (cardPlay.Target.Monster?.IntendsToAttack ?? false)
        {
            await PowerCmd.Apply<WeakPower>(choiceContext, cardPlay.Target, amount,
                Owner.Creature, this);
        }
        else
        {
            await PowerCmd.Apply<VulnerablePower>(choiceContext, cardPlay.Target, amount,
                Owner.Creature, this);
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Amount"].UpgradeValueBy(1m);
    }
}