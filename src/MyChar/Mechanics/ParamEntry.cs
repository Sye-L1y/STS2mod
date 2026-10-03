using MegaCrit.Sts2.Core.Entities.Cards;

namespace MyCharMod.Mechanics;

/// <summary>传参条目携带的效果种类。响应牌消费时逐条结算。</summary>
public enum ParamKind
{
    /// <summary>抽 N 张牌。</summary>
    Draw,

    /// <summary>回复 N 点生命。</summary>
    Heal,

    /// <summary>本次结算的这张牌重放 +N。</summary>
    Replay,

    /// <summary>响应牌的伤害 / 格挡数值 ×(1+N)（0.5 = +50%）。只放大本牌自身数值，不影响其他传参。</summary>
    EffectBoost,

    /// <summary>响应牌的伤害数值 ×(1+N)（1 = 双倍）。只放大伤害，不动格挡。</summary>
    DamageBoost,

    /// <summary>响应牌的伤害数值 **+N**（平加，乘算增幅之后再叠加）。只加伤害，不动格挡。</summary>
    DamageAdd,

    /// <summary>响应牌结算时获得 N 层协议。</summary>
    GainProtocol,

    /// <summary>给这张牌本次结算的攻击目标叠负面状态。种类见 <see cref="ParamEntry.Variant"/>，层数 = Value。</summary>
    Debuff,

    /// <summary>给这张牌永久加上一个关键词。关键词 = <c>(CardKeyword)entry.Variant</c>。</summary>
    Keyword,

    /// <summary>生成一张牌加入手牌。种类见 <see cref="ParamEntry.Variant"/>。</summary>
    GenerateCard,

    /// <summary>给手牌中**所有**牌加上一个关键词。关键词 = <c>(CardKeyword)entry.Variant</c>。</summary>
    KeywordAllHand,

    /// <summary>从 3 张本职业随机牌中选 1 张加入手牌。</summary>
    ChooseClassCard,
}

/// <summary>传参可以施加的负面状态，存在 <see cref="ParamEntry.Variant"/> 里。</summary>
public enum DebuffKind
{
    Vulnerable,
    Weak,
    Frail,
    Poison,
}

/// <summary>传参可以生成的牌，存在 <see cref="ParamEntry.Variant"/> 里。</summary>
public enum GenCardKind
{
    ProtocolAlpha,
    ProtocolBeta,
    ProtocolGamma,
    CopyThisCard,

    /// <summary>生成 N 张「小刀」（本体 Shiv，0 费消耗攻击）。Value = 张数。</summary>
    Shiv,

    /// <summary>生成 N 张「随机合法附魔的小刀」。Value = 张数。</summary>
    EnchantedShiv,

    /// <summary>生成 N 张随机无色牌。Value = 张数。</summary>
    ColorlessRandom,
}

/// <summary>
/// 传参池里的一条传参。带「传参」的牌打出时向池中加入一条，
/// 直到被响应牌消费为止；效果作用域见 <see cref="ParamKind"/>。
/// </summary>
public sealed class ParamEntry
{
    public required ParamKind Kind { get; init; }

    public required decimal Value { get; init; }

    /// <summary>子类型：负面状态 / 关键词 / 生成的牌。不同 <see cref="Kind"/> 下含义不同。</summary>
    public int Variant { get; init; }

    /// <summary>来源牌 id，仅用于调试。</summary>
    public string SourceId { get; init; } = "";

    /// <summary>传参池 tooltip 里的一行说明。</summary>
    public string Describe() => Kind switch
    {
        ParamKind.Draw => $"抽{Value:0}张牌",
        ParamKind.Heal => $"回复{Value:0}点生命",
        ParamKind.Replay => $"响应牌重放{Value:0}次",
        ParamKind.EffectBoost => $"响应牌伤害与格挡+{Value * 100m:0}%",
        ParamKind.DamageBoost => $"响应牌伤害+{Value * 100m:0}%",
        ParamKind.DamageAdd => $"响应牌伤害+{Value:0}",
        ParamKind.GainProtocol => $"获得{Value:0}层协议",
        ParamKind.Debuff => $"施加{DebuffName((DebuffKind)Variant)}{Value:0}层",
        ParamKind.Keyword => $"本牌获得「{KeywordName((CardKeyword)Variant)}」",
        ParamKind.KeywordAllHand => $"手牌全部获得「{KeywordName((CardKeyword)Variant)}」",
        ParamKind.ChooseClassCard => "从3张本职业牌中选1张加入手牌",
        ParamKind.GenerateCard => Variant switch
        {
            (int)GenCardKind.Shiv => $"加入{Value:0}张小刀",
            (int)GenCardKind.EnchantedShiv => $"加入{Value:0}张附魔小刀",
            (int)GenCardKind.ColorlessRandom => $"加入{Value:0}张随机无色牌",
            _ => $"生成「{GenCardName((GenCardKind)Variant)}」",
        },
        _ => Kind.ToString(),
    };

    private static string DebuffName(DebuffKind kind) => kind switch
    {
        DebuffKind.Vulnerable => "易伤",
        DebuffKind.Weak => "虚弱",
        DebuffKind.Frail => "脆弱",
        DebuffKind.Poison => "中毒",
        _ => kind.ToString(),
    };

    private static string KeywordName(CardKeyword keyword) => keyword switch
    {
        CardKeyword.Exhaust => "消耗",
        CardKeyword.Ethereal => "虚无",
        CardKeyword.Innate => "固有",
        CardKeyword.Retain => "保留",
        CardKeyword.Unplayable => "不能打出",
        CardKeyword.Eternal => "永恒",
        _ => keyword.ToString(),
    };

    private static string GenCardName(GenCardKind kind) => kind switch
    {
        GenCardKind.ProtocolAlpha => "α协议",
        GenCardKind.ProtocolBeta => "β协议",
        GenCardKind.ProtocolGamma => "γ协议",
        GenCardKind.CopyThisCard => "本牌的复制",
        GenCardKind.Shiv => "小刀",
        GenCardKind.EnchantedShiv => "附魔小刀",
        GenCardKind.ColorlessRandom => "随机无色牌",
        _ => kind.ToString(),
    };
}