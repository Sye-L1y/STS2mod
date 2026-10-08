using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>普通协议迭代技能：每打出 2 次（升级后 1 次），获得协议层数永久 +1（本场战斗）。</summary>
[Pool(typeof(MyCharCardPool))]
public sealed class ProtocolIteration : AlphaCard
{
    public override bool GrantsProtocol => true;

    /// <summary>迭代间隔：升级后由 2 收紧到 1，其余不变。做成动态变量以便卡面标绿。</summary>
    public override int IterationEvery =>
        DynamicVars.TryGetValue("IterEvery", out DynamicVar? every) ? (int)every.BaseValue : 2;

    public override IReadOnlyDictionary<string, decimal> IterationVarBonus =>
        new Dictionary<string, decimal> { ["Protocol"] = 1m };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Protocol", 3m),
        new DynamicVar("IterEvery", 2m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("protocol_iteration");

    public override List<(string, string)>? Localization => new CardLoc(
        "协议迭代",
        "获得{Protocol:diff()}层[gold]协议[/gold]。\n[gold]迭代[/gold]{IterEvery:inverseDiff()}（已打出{IterCount}次）：协议+1。",
        ("flavor", "权限也是可以一次次加码的。"));

    public ProtocolIteration()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        int layers = (int)ResolveValue("Protocol", IterationBonusFor("Protocol"));
        ProtocolPower.Of(this)?.AddProtocol(layers);
        return Task.CompletedTask;
    }

    protected override void OnUpgrade() => DynamicVars["IterEvery"].UpgradeValueBy(-1m);
}