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
/// 稀有能力和「厄难覆盖」：消耗[gold]协议[/gold]时，每消耗 1 层，随机给予一个敌人 3（5）层[gold]灾厄[/gold]。
/// 无论协议消耗来自回合结算还是协议消耗牌都会触发（监听 E17）。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class CalamityCoverage : AlphaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Doom", 3m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("calamity_coverage");

    public override List<(string, string)>? Localization => new CardLoc(
        "厄难覆盖",
        "消耗[gold]协议[/gold]时，每消耗1层，随机给予一个敌人{Doom:diff()}层[gold]灾厄[/gold]。",
        ("flavor", "授权撤到哪儿，灾就铺到哪儿。"));

    public CalamityCoverage()
        : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<CalamityCoveragePower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
        Owner.Creature.GetPower<CalamityCoveragePower>()?.Configure(DynamicVars["Doom"].IntValue);
    }

    protected override void OnUpgrade() => DynamicVars["Doom"].UpgradeValueBy(2m);
}