using BaseLib.Patches.Content;
using MegaCrit.Sts2.Core.Entities.Cards;

namespace MyCharMod.Mechanics;

/// <summary>
/// 阿尔法的四个自定义关键词。
///
/// BaseLib 在 ModelDb.Init 时扫描带 [CustomEnum] 的静态字段，给它们分配运行时枚举值，
/// 并把「关键词 key」记进 CustomKeywords.KeywordIDs。key = 命名空间根 + '-' + 字段名大写：
///     MyCharMod.Mechanics -> "MYCHARMOD-" -> "MYCHARMOD-RESPONSE"
/// 所以本地化要写在 card_keywords 表里（见 assets/localization/zhs/card_keywords.json）：
///     card_keywords.MYCHARMOD-RESPONSE.title / .description
///
/// [KeywordProperties(AutoKeywordPosition.After)] 决定关键词显示在卡面描述的上方还是下方，
/// 和原版的「消耗」「虚无」排在一起。
///
/// 只要把关键词挂进卡牌的 CanonicalKeywords，卡面词条和悬浮说明就都由引擎自动生成
/// （CardModel.HoverTips 会对每个关键词调 HoverTipFactory.FromKeyword）。
/// </summary>
public static class AlphaKeywords
{
    /// <summary>传参：打出时把一条效果写进传参池。</summary>
    [CustomEnum(null)]
    [KeywordProperties(AutoKeywordPosition.None)]
    public static CardKeyword ParamPassing;

    /// <summary>响应：打出时消费整池传参；池空也能打出，只是吃不到加成。</summary>
    [CustomEnum(null)]
    [KeywordProperties(AutoKeywordPosition.After)]
    public static CardKeyword Response;

    /// <summary>协议：回合开始时按层数档位生成协议牌。</summary>
    [CustomEnum(null)]
    [KeywordProperties(AutoKeywordPosition.None)]
    public static CardKeyword Protocol;

    /// <summary>迭代：每打出一次，本场战斗内永久增强。</summary>
    [CustomEnum(null)]
    [KeywordProperties(AutoKeywordPosition.None)]
    public static CardKeyword Iteration;
}