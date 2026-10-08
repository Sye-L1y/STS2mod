using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 普通技能「弱点参数」：获得 4 点[gold]活力[/gold]；[gold]传参[/gold]：响应牌伤害 +3。
/// 升级只减费（1 → 0），数值不变。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class WeaknessParam : AlphaCard
{
    public override bool IsParamSource => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Vigor", 4m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("weakness_param");

    public override List<(string, string)>? Localization => new CardLoc(
        "弱点参数",
        "获得{Vigor:diff()}点[gold]活力[/gold]。\n[gold]传参[/gold]：响应牌伤害+3。",
        ("flavor", "把对方的弱点写成一条参数，交给下一次响应。"));

    public WeaknessParam()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<VigorPower>(choiceContext, Owner.Creature,
            ResolveValue("Vigor"), Owner.Creature, this);
    }

    protected override ParamEntry? BuildParam() =>
        new() { Kind = ParamKind.DamageAdd, Value = 3m, SourceId = "WEAKNESS_PARAM" };

    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}