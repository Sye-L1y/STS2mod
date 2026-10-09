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

/// <summary>
/// 稀有能力和「终响激发」：[gold]传参[/gold]时所有敌人本回合失去 1（2）点[gold]力量[/gold]；
/// 触发[gold]响应[/gold]时所有敌人获得 5（7）层[gold]灾厄[/gold]。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class FinalEchoStimulus : AlphaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("StrengthLoss", 1m),
        new DynamicVar("Doom", 5m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("final_echo_stimulus");

    public override List<(string, string)>? Localization => new CardLoc(
        "终响激发",
        "[gold]传参[/gold]时，所有敌人本回合失去{StrengthLoss:diff()}点[gold]力量[/gold]。\n触发[gold]响应[/gold]时，所有敌人获得{Doom:diff()}层[gold]灾厄[/gold]。",
        ("flavor", "最后一声回响，替你把账目清了。"));

    public FinalEchoStimulus()
        : base(3, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // 用 Apply 的返回值写参数：它拿到的就是本次生效的那个实例（已有则返回已存在的那个）。
        FinalEchoStimulusPower? power = await PowerCmd.Apply<FinalEchoStimulusPower>(
            choiceContext, Owner.Creature, 1m, Owner.Creature, this);
        power?.Configure(DynamicVars["StrengthLoss"].IntValue, DynamicVars["Doom"].IntValue);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["StrengthLoss"].UpgradeValueBy(1m);
        DynamicVars["Doom"].UpgradeValueBy(2m);
    }
}