using System.Collections.Generic;
using System.Linq;
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
/// [gold]迭代[/gold]牌费用 −1。
/// **升级只降低门槛（3 → 2），不降费**（费用恒定 2）。
///
/// 门槛不同 = 触发条件不同，所以不能合并：同门槛的份数归并到一条状态栏上（降费额度 = 份数），
/// 基础牌与升级牌各占一条。见 <see cref="ElegantFormulaPower"/>。
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
        int threshold = DynamicVars["Threshold"].IntValue;

        // 同门槛的份数归并到同一个载体上；门槛不同（3 vs 2）则各占一条状态栏。
        // 这里必须用 GetPowerInstances（实例化后 GetPower 只返回第一个，会写到别的门槛身上）。
        ElegantFormulaPower? same = Owner.Creature
            .GetPowerInstances<ElegantFormulaPower>()
            .FirstOrDefault(power => power.IterationThreshold == threshold);
        if (same != null)
        {
            same.AddCopy();
            return;
        }

        ElegantFormulaPower? applied = await PowerCmd.Apply<ElegantFormulaPower>(
            choiceContext, Owner.Creature, 1m, Owner.Creature, this);
        applied?.Configure(threshold);
    }

    protected override void OnUpgrade()
    {
        DynamicVars["Threshold"].UpgradeValueBy(-1m);
    }
}
