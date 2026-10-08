using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Patches.Localization;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace MyCharMod.Mechanics;

/// <summary>
/// 「高阶洞察」的载体：每累计完成 <see cref="Data.IterEvery"/> 次迭代，
/// 就往传参池里塞一条「给全体敌人 N 层虚弱」（N 由打出时的升级状态决定）。
/// 走 <see cref="IAlphaIterationListener"/> 监听，不自己产出迭代。
///
/// **叠加**：<see cref="PowerStackType.Counter"/>，本牌重复打出的份数记在 <see cref="PowerModel.Amount"/>；
/// 每次触发的虚弱层数 = 卡面值 × 份数，状态栏数字即份数。
/// </summary>
public sealed class IterationBroadcastPower : CustomPowerModel, IAlphaIterationListener,
    IAddDumbVariablesToPowerDescription
{
    /// <summary>本场战斗内可变状态（Power 的单例数量承载不了层数，另存一份）。</summary>
    public sealed class Data
    {
        /// <summary>每次迭代广播的虚弱层数（升级后 2）。</summary>
        public int WeakLayers = 1;

        /// <summary>触发一次广播所需的迭代次数（初始 3，升级后 2）。</summary>
        public int IterEvery = 3;

        /// <summary>距离下一次广播已累计的迭代次数。</summary>
        public int Counter;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldScaleInMultiplayer => false;

    public override string? CustomPackedIconPath => MyCharAssets.Power("param_iteration");

    public override string? CustomBigIconPath => MyCharAssets.Power("param_iteration");

    public override List<(string, string)>? Localization => new()
    {
        ("title", "高阶洞察"),
        ("description", "每完成{IterEvery}次[gold]迭代[/gold]，就[gold]传参[/gold]：给予全体敌人{Weak}层[gold]虚弱[/gold]。"),
    };

    protected override object? InitInternalData() => new Data();

    public Data Core => GetInternalData<Data>();

    /// <summary>本牌重复打出的份数（Counter 叠层层数）：先打出的那张为 1。</summary>
    private int Copies => System.Math.Max(1, Amount);

    public void AddDumbVariablesToPowerDescription(LocString description)
    {
        description.Add("Weak", Core.WeakLayers * Copies);
        description.Add("IterEvery", Core.IterEvery);
    }

    public async Task OnAlphaIteration(PlayerChoiceContext choiceContext, CardModel card)
    {
        int every = System.Math.Max(Core.IterEvery, 1);
        Core.Counter++;
        if (Core.Counter < every)
        {
            return;
        }
        Core.Counter -= every;

        Player? player = Owner.Player;
        AlphaCorePower? core = player == null ? null : AlphaCorePower.Of(player);
        if (core != null)
        {
            await core.AddParamWithHook(choiceContext, new ParamEntry
            {
                Kind = ParamKind.DebuffAll,
                Value = Core.WeakLayers * Copies,
                Variant = (int)DebuffKind.Weak,
                SourceId = "ITERATION_BROADCAST",
            });
        }
    }
}
