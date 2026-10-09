using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Patches.Localization;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;

namespace MyCharMod.Mechanics;

/// <summary>
/// 「无限注入」的载体：己方回合开始时，往传参池里注入一条「获得 N 层协议」的传参。
///
/// 走 <see cref="AlphaCorePower.AddParamWithHook"/> 入池（所有入池入口统一走它），
/// 所以「终响激发」之类的 E18 监听者也能照常被触发。
/// 用 <c>AfterPlayerTurnStart</c>：本回合打出时回合开始已经过去，第一次注入发生在**下一个**己方回合。
///
/// **叠加**：<see cref="PowerStackType.Counter"/>，份数记在 <see cref="PowerModel.Amount"/>，
/// 每次注入的协议层数 = 份数。
/// </summary>
public sealed class InfiniteInjectionPower : CustomPowerModel, IAddDumbVariablesToPowerDescription
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Counter;

    public override bool ShouldScaleInMultiplayer => false;

    public override string? CustomPackedIconPath => MyCharAssets.Power("infinite_injection");

    public override string? CustomBigIconPath => MyCharAssets.Power("infinite_injection");

    public override List<(string, string)>? Localization => new()
    {
        ("title", "无限注入"),
        ("description", "每回合开始时，[gold]传参[/gold]：获得{Protocol}层[gold]协议[/gold]。"),
    };

    /// <summary>本牌重复打出的份数（Counter 叠层层数）：先打出的那张为 1。</summary>
    private int Copies => System.Math.Max(1, Amount);

    public void AddDumbVariablesToPowerDescription(LocString description) =>
        description.Add("Protocol", Copies);

    public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
    {
        if (player.Creature != Owner)
        {
            return;
        }
        AlphaCorePower? core = AlphaCorePower.Of(player);
        if (core == null)
        {
            return;
        }
        await core.AddParamWithHook(choiceContext, new ParamEntry
        {
            Kind = ParamKind.GainProtocol,
            Value = Copies,
            SourceId = "INFINITE_INJECTION",
        });
    }
}
