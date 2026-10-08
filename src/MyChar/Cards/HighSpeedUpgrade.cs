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
/// 稀有技能「高速升级」：接下来打出的 1（2）张[gold]迭代[/gold]牌，迭代计数额外 +2。
/// 不限回合，跨回合保留，直到次数用完。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class HighSpeedUpgrade : AlphaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Count", 1m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("fast_iteration");

    public override List<(string, string)>? Localization => new CardLoc(
        "高速升级",
        "接下来打出的{Count:diff()}张[gold]迭代[/gold]牌，迭代计数额外+2。",
        ("flavor", "把升级包直接推进下一个循环里。"));

    public HighSpeedUpgrade()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<HighSpeedUpgradePower>(choiceContext, Owner.Creature,
            DynamicVars["Count"].BaseValue, Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["Count"].UpgradeValueBy(1m);
}