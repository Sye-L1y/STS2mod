using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 起始遗物「自我升级」。
///
/// 职责：
///   1. 战斗开始时挂上两个**可见 Power**：传参池（<see cref="AlphaCorePower"/>）与
///      协议层数（<see cref="ProtocolPower"/>），这样状态栏才能看到池子和层数；
///   2. 战斗内每 3 次响应或迭代，获得 **1 层协议**（计数与发放逻辑在 <see cref="AlphaCorePower"/> 里）；
///   3. 遗物图标上的计数条显示当前进度（0 / 1 / 2，原版计数条样式）。
///
/// 通过「欧洛巴斯之触」升级为古老版 <see cref="MyCharStarterRelicAncient"/>（每满 3 次给 2 层协议）。
/// </summary>
[Pool(typeof(MyCharRelicPool))]
public sealed class MyCharStarterRelic : CustomRelicModel, IAlphaProtocolMilestone
{
    public override RelicRarity Rarity => RelicRarity.Starter;

    /// <summary>兜底图标：真图标在 ModEntry.RegisterRelicIcons 注册；没注册上时退回「燃烧之血」。</summary>
    protected override string IconBaseName => "burning_blood";

    public override List<(string, string)>? Localization => new()
    {
        ("title", "自我升级"),
        ("description", "战斗内每完成3次[gold]响应[/gold]或[gold]迭代[/gold]，获得1层[gold]协议[/gold]。"),
        ("flavor", "每一次应答，都让它更接近完美。"),
    };

    /// <summary>遗物计数条：当前响应/迭代进度（0 / 1 / 2）。</summary>
    public override bool ShowCounter => true;

    public override int DisplayAmount => AlphaCorePower.Of(Owner)?.CounterProgress ?? 0;

    /// <summary>每满 3 次响应/迭代发放 1 层协议。</summary>
    public int ProtocolPerMilestone => 1;

    /// <summary>响应/迭代计数变化时由 <see cref="AlphaCorePower"/> 调回来刷新计数条。</summary>
    public void NotifyCounterChanged() => InvokeDisplayAmountChanged();

    /// <summary>「欧洛巴斯之触」把起始遗物替换成古老版（每满 3 次给 2 层协议）。</summary>
    public override RelicModel? GetUpgradeReplacement() => ModelDb.Relic<MyCharStarterRelicAncient>();

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