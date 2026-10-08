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
/// 罕见技能「协议发掘」：获得协议，并把「获得协议」写进传参池，供响应牌消费（自身不响应）。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class ProtocolResponse : AlphaCard
{
    public override bool IsParamSource => true;

    public override bool GrantsProtocol => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Protocol", 3m),
        new DynamicVar("Param", 2m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("protocol_response");

    public override List<(string, string)>? Localization => new CardLoc(
        "协议发掘",
        "获得{Protocol:diff()}层[gold]协议[/gold]。\n[gold]传参[/gold]：获得{Param:diff()}层[gold]协议[/gold]。",
        ("flavor", "每一次应答，都在给下一次授权。"));

    public ProtocolResponse()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ProtocolPower.Of(this)?.AddProtocol((int)ResolveValue("Protocol"));
        return Task.CompletedTask;
    }

    protected override ParamEntry? BuildParam() =>
        new() { Kind = ParamKind.GainProtocol, Value = DynamicVars["Param"].BaseValue, SourceId = "PROTOCOL_RESPONSE" };

    protected override void OnUpgrade()
    {
        DynamicVars["Protocol"].UpgradeValueBy(1m);
        DynamicVars["Param"].UpgradeValueBy(1m);
    }
}