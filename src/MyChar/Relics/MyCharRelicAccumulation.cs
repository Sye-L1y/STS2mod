using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models.Powers;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>稀有遗物「蕴纳」：回合开始时，每有一条传参获得 1 点活力。</summary>
[Pool(typeof(MyCharRelicPool))]
public sealed class MyCharRelicAccumulation : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Rare;

    protected override string IconBaseName => "anchor";

    public override List<(string, string)>? Localization => new()
    {
        ("title", "蕴纳"),
        ("description", "回合开始时，每有一条[gold]传参[/gold]，获得1点[gold]活力[/gold]。"),
        ("flavor", "攒下的每一条参数，都在暗处发着热。"),
    };

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner.Creature)
        {
            return;
        }

        int count = AlphaCorePower.Of(Owner)?.ParamCount ?? 0;
        if (count <= 0)
        {
            return;
        }

        Flash();
        await PowerCmd.Apply<VigorPower>(choiceContext, Owner.Creature, count, Owner.Creature, null);
    }
}