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

/// <summary>
/// 罕见能力「甲胄铸造」：每完成 2（升级 1）次[gold]迭代[/gold]，获得 1 层[gold]覆甲[/gold]。
/// 由 <see cref="ArmorForgingPower"/> 监听迭代完成事件（E3）。
///
/// 每次打出生成一个**独立**载体（见 <see cref="ArmorForgingPower.InstanceType"/>），
/// 所以要把阈值写到**本次返回的那个实例**上，不能写 <c>GetPower&lt;T&gt;()</c> 拿到的第一个。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class ArmorForging : AlphaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("IterEvery", 2m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("armor_forging");

    public override List<(string, string)>? Localization => new CardLoc(
        "甲胄铸造",
        "每完成{IterEvery:inverseDiff()}次[gold]迭代[/gold]，获得1层[gold]覆甲[/gold]。",
        ("flavor", "每闭合一次循环，就多铸一片甲。"));

    public ArmorForging()
        : base(1, CardType.Power, CardRarity.Uncommon, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArmorForgingPower? power = await PowerCmd.Apply<ArmorForgingPower>(
            choiceContext, Owner.Creature, 1m, Owner.Creature, this);
        if (power != null)
        {
            power.Core.IterEvery = (int)DynamicVars["IterEvery"].BaseValue;
        }
    }

    protected override void OnUpgrade() => DynamicVars["IterEvery"].UpgradeValueBy(-1m);
}
