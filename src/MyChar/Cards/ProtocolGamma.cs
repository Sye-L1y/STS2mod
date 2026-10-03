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

/// <summary>协议牌：回合开始时由「协议」层数生成，0 费、消耗、虚无。衍生牌，挂在专属池（非角色池）。</summary>
[Pool(typeof(MyCharTokenCardPool))]
public sealed class ProtocolGamma : AlphaCard
{
    public override bool IsParamSource => true;

    protected override IEnumerable<CardKeyword> BaseKeywords =>
        new[] { CardKeyword.Exhaust, CardKeyword.Ethereal };

    /// <summary>响应牌重放 1，升级 +1 → 2。</summary>
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Replay", 1m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("protocol_gamma");

    public override List<(string, string)>? Localization => new CardLoc(
        "γ协议",
        "获得3点[gold]能量[/gold]，抽3张牌。\n[gold]传参[/gold]：响应牌获得重放{Replay:diff()}。",
        ("flavor", "最高权限，代价自明。"));

    public ProtocolGamma()
        : base(0, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayerCmd.GainEnergy(3m, Owner);
        await CardPileCmd.Draw(choiceContext, 3m, Owner);
        await AlphaHooks.ProtocolTriggered(choiceContext, this);
    }

    protected override ParamEntry? BuildParam() =>
        new() { Kind = ParamKind.Replay, Value = DynamicVars["Replay"].BaseValue, SourceId = "PROTOCOL_GAMMA" };

    protected override void OnUpgrade()
    {
        DynamicVars["Replay"].UpgradeValueBy(1m);
    }
}