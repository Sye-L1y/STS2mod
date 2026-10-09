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
/// **独立实例**（<see cref="PowerInstanceType.Instanced"/>）：本牌每打出一次就多一个独立载体，
/// 各自记自己的 <see cref="Data.Counter"/> 与 <see cref="Data.IterEvery"/>（升级后 2、未升级 3）、
/// 以及 <see cref="Data.WeakLayers"/>（升级后 2、未升级 1）。
/// 不能合并——合并会让两张不同升级档的牌共用同一个计数器和阈值。
/// </summary>
public sealed class IterationBroadcastPower : CustomPowerModel, IAlphaIterationListener,
    IAddDumbVariablesToPowerDescription
{
    /// <summary>本载体自己的对战内状态。</summary>
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

    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public override PowerStackType StackType => PowerStackType.Single;

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

    public void AddDumbVariablesToPowerDescription(LocString description)
    {
        description.Add("Weak", Core.WeakLayers);
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
            core.BeginParamBatch();
            await core.AddParamWithHook(choiceContext, new ParamEntry
            {
                Kind = ParamKind.DebuffAll,
                Value = Core.WeakLayers,
                Variant = (int)DebuffKind.Weak,
                SourceId = "ITERATION_BROADCAST",
            });
        }
    }
}
