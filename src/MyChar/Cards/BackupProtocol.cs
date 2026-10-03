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

/// <summary>罕见技能：当场拿到协议层数，再把「抽一张」留到下回合补节奏。</summary>
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
        "获得{Protocol:diff()}层[gold]协议[/gold]。下回合抽1张牌。",
        ("flavor", "总得留一条后路，等下回合再走。"));

    public BackupProtocol()
        : base(1, CardType.Skill, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ProtocolPower.Of(this)?.AddProtocol((int)DynamicVars["Protocol"].BaseValue);
        await PowerCmd.Apply<PendingDrawPower>(choiceContext, Owner.Creature, 1m,
            Owner.Creature, this);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Protocol"].UpgradeValueBy(1m);
    }
}