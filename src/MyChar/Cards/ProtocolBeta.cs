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
public sealed class ProtocolBeta : AlphaCard
{
    public override bool IsParamSource => true;

    protected override IEnumerable<CardKeyword> BaseKeywords =>
        new[] { CardKeyword.Exhaust, CardKeyword.Ethereal };

    /// <summary>回血 2，升级 +2 → 4。</summary>
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Heal", 2m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("protocol_beta");

    public override List<(string, string)>? Localization => new CardLoc(
        "β协议",
        "获得2点[gold]能量[/gold]，抽2张牌。\n[gold]传参[/gold]：回复{Heal:diff()}点生命。",
        ("flavor", "中等权限，开始有点温度。"));

    public ProtocolBeta()
        : base(0, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayerCmd.GainEnergy(2m, Owner);
        await CardPileCmd.Draw(choiceContext, 2m, Owner);
        await AlphaHooks.ProtocolTriggered(choiceContext, this);
    }

    protected override ParamEntry? BuildParam() =>
        new() { Kind = ParamKind.Heal, Value = DynamicVars["Heal"].BaseValue, SourceId = "PROTOCOL_BETA" };

    protected override void OnUpgrade()
    {
        DynamicVars["Heal"].UpgradeValueBy(2m);
    }
}