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
/// 就往传参池里塞一条「给全体敌人 1（2）层[gold]虚弱[/gold]」。
/// 由 <see cref="IterationBroadcastPower"/> 监听迭代完成事件（E3）。
///
/// 每次打出生成一个**独立**载体（见 <see cref="IterationBroadcastPower.InstanceType"/>）：
/// 升级前后的迭代阈值不同（3 / 2），必须各记各的计数器，不能合并成一条。
/// 所以阈值要写到**本次返回的那个实例**上，不能写 <c>GetPower&lt;T&gt;()</c> 拿到的第一个。
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
        IterationBroadcastPower? power = await PowerCmd.Apply<IterationBroadcastPower>(
            choiceContext, Owner.Creature, 1m, Owner.Creature, this);
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
