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

    /// <summary>占位阶段复用铁甲战士的能量图标；有真图标后改成自定义路径（见 docs/02）。</summary>
    public override string EnergyColorName => "ironclad";

    /// <summary>卡背/图鉴里这张卡所属池子的主色。</summary>
    public override Color DeckEntryCardColor => new("D62000");

    /// <summary>能量数字描边色，要和能量图标搭。</summary>
    public override Color EnergyOutlineColor => new("802020");

    /// <summary>卡框颜色（BaseLib 会用 HSV 生成卡框材质）。</summary>
    public override Color ShaderColor => new("D62000");

    public override bool IsColorless => false;

    /// <summary>true = 图鉴里默认已解锁显示，不用先"见过"。</summary>
    public override bool SeenByDefault => true;
}