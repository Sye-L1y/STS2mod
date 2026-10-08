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
/// 罕见能力「高阶洞察」：本场战斗中，每当迭代牌完成 {IterEvery}（升级 2）次[gold]迭代[/gold]，
/// 就往传参池里塞一条「给全体敌人 1（2） 层[gold]虚弱[/gold]一次」。
/// 由 <see cref="IterationBroadcastPower"/> 监听迭代完成事件（E3）。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class IterationBroadcast : AlphaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Weak", 1m),
        new DynamicVar("IterEvery", 3m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("param_iteration");

    public override List<(string, string)>? Localization => new CardLoc(
        "高阶洞察",
        "每完成{IterEvery:inverseDiff()}次[gold]迭代[/gold]，就[gold]传参[/gold]：给予全体敌人{Weak:diff()}层[gold]虚弱[/gold]。",
        ("flavor", "每一次循环闭合，都向所有终端广播一条指令。"));

    public IterationBroadcast()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<IterationBroadcastPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
        IterationBroadcastPower? power = Owner.Creature.GetPower<IterationBroadcastPower>();
        if (power != null)
        {
            power.Core.WeakLayers = (int)DynamicVars["Weak"].BaseValue;
            power.Core.IterEvery = (int)DynamicVars["IterEvery"].BaseValue;
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Weak"].UpgradeValueBy(1m);
        DynamicVars["IterEvery"].UpgradeValueBy(-1m);
    }
}