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

/// <summary>罕见技能：当场拿到协议层数，下回合协议结算后按该回合消耗总量的一半返还协议。</summary>
[Pool(typeof(MyCharCardPool))]
public sealed class BackupProtocol : AlphaCard
{
    public override bool GrantsProtocol => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Protocol", 3m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("backup_protocol");

    public override List<(string, string)>? Localization => new CardLoc(
        "备用协议",
        "获得{Protocol:diff()}层[gold]协议[/gold]。下回合[gold]协议[/gold]结算后，返还该回合结算消耗量一半的[gold]协议[/gold]。",
        ("flavor", "总得留一条后路，等下回合再走。"));

    public BackupProtocol()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ProtocolPower.Of(this)?.AddProtocol((int)DynamicVars["Protocol"].BaseValue);
        // 下回合协议结算后（AfterPlayerTurnStartLate），按该回合实际消耗总量的一半返还。
        await PowerCmd.Apply<ProtocolRefundPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Protocol"].UpgradeValueBy(1m);
    }
}