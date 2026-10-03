using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 罕见能力「协议共振」：**被动**——之后每次获得[gold]协议[/gold]，额外获得 1 层。
/// 升级只减费（2 → 1），不改变增幅层数。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class ProtocolResonance : AlphaCard
{
    public override bool GrantsProtocol => true;

    public override string? CustomPortraitPath => MyCharAssets.Card("protocol_resonance");

    public override List<(string, string)>? Localization => new CardLoc(
        "协议共振",
        "之后每次获得[gold]协议[/gold]时，额外获得1层[gold]协议[/gold]。",
        ("flavor", "权限自己会共振，只要你搭好回路。"));

    public ProtocolResonance()
        : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<ProtocolResonancePower>(choiceContext, Owner.Creature,
            1m, Owner.Creature, this);
    }

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}