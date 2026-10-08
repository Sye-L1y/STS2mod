using BaseLib.Abstracts;
using Godot;

namespace MyCharMod;

/// <summary>
/// 角色的卡池。卡牌通过类上的 [Pool(typeof(MyCharCardPool))] 自动挂进这个池子，
/// 所以这里不需要手写卡牌清单（GenerateAllCards 用基类默认的空数组即可）。
///
/// 类名 -> ModelId "card_pool.MY_CHAR_CARD_POOL"
/// </summary>
public sealed class MyCharCardPool : CustomCardPoolModel
{
    /// <summary>卡图/图集路径里会用到的池子名，只能用 [a-z0-9_]。</summary>
    public override string Title => "my_char";

    // EnergyColorName 故意不覆盖：基类给的是 "card_pool∴MY_CHAR_CARD_POOL" 这种带分隔符的形式，
    // 只有这种形式才会被 BaseLib 的 EnergyIconHelper.GetPath 补丁接管，换成下面两张自定义图标。
    // 覆盖成 "ironclad" 会退回本体图标。

    /// <summary>卡面费用 / 大能量图标。</summary>
    public override string? BigEnergyIconPath => MyCharAssets.EnergyBig;

    /// <summary>描述内联 [E] 能量图标。</summary>
    public override string? TextEnergyIconPath => MyCharAssets.EnergyText;

    /// <summary>卡背/图鉴里这张卡所属池子的主色。</summary>
    public override Color DeckEntryCardColor => new("D62000");

    /// <summary>能量数字描边色，与卡框（ShaderColor）保持一致。</summary>
    public override Color EnergyOutlineColor => new("c4d8f6");

    /// <summary>卡框颜色（BaseLib 会用 HSV 生成卡框材质）。</summary>
    public override Color ShaderColor => new("c4d8f6");

    public override bool IsColorless => false;

    /// <summary>true = 图鉴里默认已解锁显示，不用先"见过"。</summary>
    public override bool SeenByDefault => true;
}