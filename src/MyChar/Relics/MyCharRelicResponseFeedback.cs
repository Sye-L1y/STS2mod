using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>普通遗物「响应反馈」：每次触发响应时获得 2 点格挡。</summary>
[Pool(typeof(MyCharRelicPool))]
public sealed class MyCharRelicResponseFeedback : CustomRelicModel, IAlphaResponseListener
{
    public override RelicRarity Rarity => RelicRarity.Common;

    /// <summary>兜底图标：真图标在 ModEntry.RegisterRelicIcons 注册。</summary>
    protected override string IconBaseName => "anchor";

    public override List<(string, string)>? Localization => new()
    {
        ("title", "响应反馈"),
        ("description", "每次触发[gold]响应[/gold]时，获得2点[gold]格挡[/gold]。"),
        ("flavor", "每一次应答，都会折返成一道护盾。"),
    };

    public async Task OnAlphaResponse(PlayerChoiceContext choiceContext, CardModel card)
    {
        Flash();
        await CreatureCmd.GainBlock(Owner.Creature, 2m, ValueProp.Unpowered, null);
    }
}