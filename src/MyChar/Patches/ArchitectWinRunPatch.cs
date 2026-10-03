using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace MyCharMod.Patches;

/// <summary>
/// 建筑师胜利兜底补丁（B9）。
///
/// 现象：打完建筑师点「继续」卡死，日志抛
/// "EnterNextAct called at victory room! WinRun should be called instead"。
/// 根因：第三方 mod WatcherMod 把 TheArchitect.WinRun 改写成「进入下一幕」，
/// MaidenSuccubus 的 FourthActEndingPatch 随后在胜利房调用 RunManager.EnterNextAct()，
/// 命中引擎的胜利房断言（见 ref/sts2/.../RunManager.cs:1298）。
///
/// 为什么不直接和 WatcherMod 抢 TheArchitect.WinRun 的前缀顺序：Harmony 前缀顺序在
/// 双方都声明高优先级时不保证，抢不过就仍然崩。这里改在更下游、且没有别人打补丁的
/// RunManager.EnterNextAct 上兜底——只要是 MY_CHAR 的胜利房，就把「进下一幕」纠正为
/// 「通关 WinRun」，与第三方 mod 的前缀顺序无关。
/// </summary>
[HarmonyPatch(typeof(RunManager), nameof(RunManager.EnterNextAct))]
internal static class ArchitectWinRunPatch
{
    private static readonly PropertyInfo? StateProperty =
        AccessTools.Property(typeof(RunManager), "State");

    [HarmonyPrefix]
    private static bool Prefix(RunManager __instance, ref Task __result)
    {
        RunState? state = StateProperty?.GetValue(__instance) as RunState;
        if (state?.CurrentRoom?.IsVictoryRoom != true)
        {
            return true;
        }

        string myCharEntry = ModelDb.Character<MyChar>().Id.Entry;
        if (!state.Players.Any(player => player.Character.Id.Entry == myCharEntry))
        {
            return true;
        }

        Log.Info("[MyChar] victory room: EnterNextAct redirected to WinRun (third-party mod conflict fallback).");
        __result = __instance.WinRun();
        return false;
    }
}