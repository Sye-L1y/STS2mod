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

/// <summary>罕见能力「渗透」：触发[gold]响应[/gold]后，对所有敌人造成 6（8）伤。</summary>
[Pool(typeof(MyCharCardPool))]
public sealed class Infiltration : AlphaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("PowerDamage", 6m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("infiltration");

    public override List<(string, string)>? Localization => new CardLoc(
        "渗透",
        "触发[gold]响应[/gold]后，对所有敌人造成{PowerDamage:diff()}点伤害。",
        ("flavor", "响应是信号，渗透是它带来的后果。"));

    public Infiltration()
        : base(2, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<InfiltrationPower>(choiceContext, Owner.Creature,
            ResolveValue("PowerDamage"), Owner.Creature, this);
    }

    protected override void OnUpgrade() => DynamicVars["PowerDamage"].UpgradeValueBy(2m);
}