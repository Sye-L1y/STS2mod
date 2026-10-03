using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>普通协议来源：1 费换 2 层协议；升级改为减费（1 → 0），协议层数不变。</summary>
[Pool(typeof(MyCharCardPool))]
public sealed class ProtocolSample : AlphaCard
{
    public override bool GrantsProtocol => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Protocol", 2m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("protocol_sample");

    public override List<(string, string)>? Localization => new CardLoc(
        "协议采样",
        "获得{Protocol:diff()}层[gold]协议[/gold]。",
        ("flavor", "采一点样本，够下一档就行。"));

    public ProtocolSample()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ProtocolPower.Of(this)?.AddProtocol((int)DynamicVars["Protocol"].BaseValue);
        return Task.CompletedTask;
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
    }
}