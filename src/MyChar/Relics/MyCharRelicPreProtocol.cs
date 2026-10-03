using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 罕见遗物「预协议」：战斗开始时获得 4 层协议。
///
/// 走 <c>AfterPlayerTurnStartLate</c>（排在协议结算之后），首个回合只发一次，
/// 这样这 4 层能留到玩家手里继续叠，而不是当回合立刻被结算掉。
/// </summary>
[Pool(typeof(MyCharRelicPool))]
public sealed class MyCharRelicPreProtocol : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Uncommon;

    protected override string IconBaseName => "anchor";

    public override List<(string, string)>? Localization => new()
    {
        ("title", "预协议"),
        ("description", "战斗开始时，获得4层[gold]协议[/gold]。"),
        ("flavor", "还没开打，授权就已经预支好了。"),
    };

    public override async Task AfterPlayerTurnStartLate(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner.Creature)
        {
            return;
        }

        await AlphaCombatSetup.EnsureCorePowers(choiceContext, Owner);

        AlphaCorePower? core = AlphaCorePower.Of(Owner);
        if (core == null || core.Core.PreProtocolGranted)
        {
            return;
        }
        core.Core.PreProtocolGranted = true;

        Flash();
        ProtocolPower.Of(Owner)?.AddProtocol(4);
    }
}