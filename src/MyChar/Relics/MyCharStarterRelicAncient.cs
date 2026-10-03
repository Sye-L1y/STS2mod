using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 起始遗物的古老版「自我进化」。通过「欧洛巴斯之触」由 <see cref="MyCharStarterRelic"/> 替换而来。
///
/// 与普通版同构，只把每次里程碑发放的协议层数从 1 提到 **2**。
/// </summary>
[Pool(typeof(MyCharRelicPool))]
public sealed class MyCharStarterRelicAncient : CustomRelicModel, IAlphaProtocolMilestone
{
    public override RelicRarity Rarity => RelicRarity.Ancient;

    /// <summary>兜底图标：真图标在 ModEntry.RegisterRelicIcons 注册。</summary>
    protected override string IconBaseName => "burning_blood";

    public override List<(string, string)>? Localization => new()
    {
        ("title", "自我进化"),
        ("description", "战斗内每完成3次[gold]响应[/gold]或[gold]迭代[/gold]，获得2层[gold]协议[/gold]。"),
        ("flavor", "旧躯壳剥落，新协议在其中成形。"),
    };

    /// <summary>遗物计数条：当前响应/迭代进度（0 / 1 / 2）。</summary>
    public override bool ShowCounter => true;

    public override int DisplayAmount => AlphaCorePower.Of(Owner)?.CounterProgress ?? 0;

    /// <summary>每满 3 次响应/迭代发放 2 层协议。</summary>
    public int ProtocolPerMilestone => 2;

    public void NotifyCounterChanged() => InvokeDisplayAmountChanged();

    public override async Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side,
        IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        if (!participants.Contains(Owner.Creature))
        {
            return;
        }
        await AlphaCombatSetup.EnsureCorePowers(choiceContext, Owner);
    }
}