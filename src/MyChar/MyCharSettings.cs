using System;
using Godot;
using MegaCrit.Sts2.Core.Logging;

namespace MyCharMod;

/// <summary>
/// 模组的全局设置（用户定：**全局记忆**，所有存档通用、重启游戏仍生效）。
///
/// 存储：<c>user://mychar_settings.cfg</c>（Godot <see cref="ConfigFile"/> 文本格式，想手改也行）。
/// 目前两项：
///   visuals/damaged_portraits —— 局内是否启用战损立绘切换（战斗 / 商店 / 休息统一受它控制）；
///   visuals/select_art        —— 选人界面立绘版本（存的是版本**标签**，如 2 / 2+ / 3 / 3+）。
///
/// 两项都由选人界面上的开关面板改（见 <c>Patches/SelectScreenPanelPatch.cs</c>），
/// 改完立刻写盘 → 下一次进选人界面还是上次的选择。
/// </summary>
internal static class MyCharSettings
{
    private const string FilePath = "user://mychar_settings.cfg";
    private const string Section = "visuals";
    private const string KeyDamaged = "damaged_portraits";
    private const string KeySelectArt = "select_art";

    /// <summary>选人界面可选立绘（数组顺序 = 按钮切换顺序：2 → 2+ → 3 → 3+）。</summary>
    /// <param name="Label">按钮上显示的名字；同时也是持久化的键（存档存标签不存下标，重排版本不会串档）。</param>
    /// <param name="Path">贴图路径。</param>
    /// <param name="Local">是不是本地专有素材（不进公开仓库，缺失时切换自动跳过）。</param>
    internal readonly record struct SelectArtEntry(string Label, string Path, bool Local);

    internal static readonly SelectArtEntry[] SelectArts =
    {
        new("2", MyCharAssets.SelectBg, Local: false),
        new("2+", MyCharAssets.SelectBg2Nsfw, Local: true),
        new("3", MyCharAssets.SelectBg3, Local: false),
        new("3+", MyCharAssets.SelectBg3Nsfw, Local: true),
    };

    /// <summary>默认选人立绘 = 「2」（改造前一直用的那张）。</summary>
    private const int DefaultSelectArt = 0;

    private static bool _loaded;

    /// <summary>局内是否启用战损立绘切换。默认开。</summary>
    public static bool DamagedPortraits { get; private set; } = true;

    /// <summary>选人界面立绘版本（索引指向 <see cref="SelectArts"/>）。</summary>
    public static int SelectArt { get; private set; } = DefaultSelectArt;

    /// <summary>任一设置变化后触发（界面据此刷新按钮文案）。</summary>
    public static event Action? Changed;

    /// <summary>当前选人立绘的显示名。</summary>
    public static string SelectArtLabel => SelectArts[SelectArt].Label;

    /// <summary>
    /// 读取配置。文件不存在 / 损坏都按默认值走，不抛异常——玩家手改坏了不该进不去游戏。
    /// </summary>
    public static void EnsureLoaded()
    {
        if (_loaded)
        {
            return;
        }

        _loaded = true;

        ConfigFile config = new();
        Error error = config.Load(FilePath);
        if (error != Error.Ok)
        {
            // 第一次进游戏就是这个结果（文件还没生成），属正常，不用打日志。
            return;
        }

        DamagedPortraits = (bool)config.GetValue(Section, KeyDamaged, true);

        // 存的是版本标签（如 "2+"）。旧版存的是下标数字，解析不出来 → 自动回落默认，
        // 不会因为版本表重排就把玩家切到别的立绘上。
        Variant saved = config.GetValue(Section, KeySelectArt, SelectArts[DefaultSelectArt].Label);
        SelectArt = IndexOfLabel(saved.AsString());

        Log.Info($"[MyChar] settings loaded: damaged={DamagedPortraits} selectArt={SelectArtLabel}");
    }

    /// <summary>按标签找下标；找不到（旧存档的数字、手改坏）返回默认。</summary>
    private static int IndexOfLabel(string? label)
    {
        for (int i = 0; i < SelectArts.Length; i++)
        {
            if (SelectArts[i].Label == label)
            {
                return i;
            }
        }

        return DefaultSelectArt;
    }

    /// <summary>切换「局内战损立绘」总开关。</summary>
    public static void ToggleDamagedPortraits()
    {
        EnsureLoaded();
        DamagedPortraits = !DamagedPortraits;
        Save();
        Changed?.Invoke();
        Log.Info($"[MyChar] damaged portraits -> {DamagedPortraits}");
    }

    /// <summary>
    /// 循环切到下一张**实际存在**的选人立绘（本地专有差分不在时自动跳过）。
    /// </summary>
    public static void CycleSelectArt()
    {
        EnsureLoaded();

        for (int step = 1; step <= SelectArts.Length; step++)
        {
            int next = (SelectArt + step) % SelectArts.Length;
            if (MyCharAssets.Exists(SelectArts[next].Path))
            {
                SelectArt = next;
                break;
            }
        }

        Save();
        Changed?.Invoke();
        Log.Info($"[MyChar] select art -> {SelectArtLabel}");
    }

    /// <summary>把当前值写回配置文件；失败只记日志（比如磁盘只读），不影响本次游戏内生效。</summary>
    private static void Save()
    {
        ConfigFile config = new();
        config.Load(FilePath); // 保留以后可能加进来的别的键
        config.SetValue(Section, KeyDamaged, DamagedPortraits);
        config.SetValue(Section, KeySelectArt, SelectArts[SelectArt].Label);

        Error error = config.Save(FilePath);
        if (error != Error.Ok)
        {
            Log.Error($"[MyChar] settings save failed: {error}");
        }
    }
}
