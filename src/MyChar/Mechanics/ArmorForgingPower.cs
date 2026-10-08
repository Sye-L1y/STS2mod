using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Patches.Localization;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace MyCharMod.Mechanics;

/// <summary>
/// 「甲胄铸造」的载体：每累计完成 <see cref="Data.IterEvery"/> 次迭代，就获得 1 层覆甲。
/// 走 <see cref="IAlphaIterationListener"/> 监听，不自己产出迭代。
///
/// **独立实例**（<see cref="PowerInstanceType.Instanced"/>）：本牌每打出一次就多一个独立载体，
/// 各自记自己的 <see cref="Data.Counter"/> 与 <see cref="Data.IterEvery"/>（升级后 1、未升级 2）。
/// 不能合并——合并会让两张不同升级档的牌共用同一个计数器和阈值。
/// </summary>
public sealed class ArmorForgingPower : CustomPowerModel, IAlphaIterationListener,
    IAddDumbVariablesToPowerDescription
{
    /// <summary>本载体自己的对战内状态。</summary>
    public sealed class Data
    {
        /// <summary>触发一次覆甲所需的迭代次数（初始 2，升级后 1）。</summary>
        public int IterEvery = 2;

        /// <summary>距离下一次覆甲已累计的迭代次数。</summary>
        public int Counter;
    }

    public override PowerType Type => PowerType.Buff;

    public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

    public override PowerStackType StackType => PowerStackType.Single;

    public override bool ShouldScaleInMultiplayer => false;

    public override string? CustomPackedIconPath => MyCharAssets.Power("armor_forging");

    public override string? CustomBigIconPath => MyCharAssets.Power("armor_forging");

    public override List<(string, string)>? Localization => new()
    {
        ("title", "甲胄铸造"),
        ("description", "每完成{IterEvery}次[gold]迭代[/gold]，获得1层[gold]覆甲[/gold]。"),
    };

    protected override object? InitInternalData() => new Data();

    public Data Core => GetInternalData<Data>();

    public void AddDumbVariablesToPowerDescription(LocString description) =>
        description.Add("IterEvery", Core.IterEvery);

    public async Task OnAlphaIteration(PlayerChoiceContext choiceContext, CardModel card)
    {
        int every = System.Math.Max(Core.IterEvery, 1);
        Core.Counter++;
        if (Core.Counter < every)
        {
            return;
        }
        Core.Counter -= every;
        await PowerCmd.Apply<PlatingPower>(choiceContext, Owner, 1m, Owner, null);
    }
}
