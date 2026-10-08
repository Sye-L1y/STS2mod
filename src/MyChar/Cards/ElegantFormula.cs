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
/// 稀有能力和「优美公式」：本场战斗中，所有**已完成[gold]迭代[/gold]次数 ≥ 3（升级后 2）**的
/// [gold]迭代[/gold]牌费用 −1。升级只减费（2 → 1）并降低门槛（3 → 2）。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class ElegantFormula : AlphaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Threshold", 3m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("condense_qi");

    public override List<(string, string)>? Localization => new CardLoc(
        "优美公式",
        "本场战斗中，所有已完成[gold]迭代[/gold]次数≥{Threshold:inverseDiff()}的[gold]迭代[/gold]牌费用-1。",
        ("flavor", "把重复的部分约掉，式子自然就短了。"));

    public ElegantFormula()
        : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<ElegantFormulaPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
        Owner.Creature.GetPower<ElegantFormulaPower>()?.Configure(DynamicVars["Threshold"].IntValue);
    }

    protected override void OnUpgrade()
    {
        EnergyCost.UpgradeBy(-1);
        DynamicVars["Threshold"].UpgradeValueBy(-1m);
    }
}