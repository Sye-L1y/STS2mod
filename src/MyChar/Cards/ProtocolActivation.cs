using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 稀有技能：先灌入协议层数，再**立刻**触发一次协议结算（不等下回合开始）。
/// 用来在同一回合内把攒到的层数变现。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class ProtocolActivation : AlphaCard
{
    public override bool GrantsProtocol => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Protocol", 3m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("protocol_activation");

    public override List<(string, string)>? Localization => new CardLoc(
        "协议激活",
        "获得{Protocol:diff()}层[gold]协议[/gold]，然后立刻结算一次[gold]协议[/gold]。",
        ("flavor", "不等回合开始，现在就执行。"));

    public ProtocolActivation()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ProtocolPower? protocol = ProtocolPower.Of(this);
        if (protocol == null)
        {
            return;
        }

        protocol.AddProtocol((int)DynamicVars["Protocol"].BaseValue);
        await protocol.TriggerProtocol(choiceContext, Owner);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Protocol"].UpgradeValueBy(1m);
    }
}