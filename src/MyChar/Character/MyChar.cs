using System.Collections.Generic;
using BaseLib.Abstracts;
using Godot;
using MegaCrit.Sts2.Core.Entities.Characters;
using MegaCrit.Sts2.Core.Models;

namespace MyCharMod;

/// <summary>
/// 新角色本体。
///
/// 继承 BaseLib 的 PlaceholderCharacterModel：所有立绘 / 图标 / 动画 / 音效默认复用铁甲战士(ironclad)的资源，
/// 所以第一阶段不需要任何美术资源也能在游戏里跑起来（占位图方案）。
/// 后续做好真美术后，把基类改成 CustomCharacterModel，再逐个 override Custom* 路径即可。
///
/// 类名决定 id：MyChar -> ModelId = "character.MY_CHAR"，本地化 key 为 characters.MY_CHAR.*
/// </summary>
public sealed class MyChar : PlaceholderCharacterModel
{
    public override string PlaceholderID => "ironclad";

    // —— 自定义美术：贴图类 + 场景类 ——
    // 场景类资源（src/MyChar/scenes/*.tscn，静态立绘版）由 PckPacker 之外的 tools/pck_append.py 追加进 pck。
    // 场景里 body 是 Sprite2D 而非 SpineSprite：战斗/火堆本体代码会自动走无 Spine 分支；
    // 商店需要 StaticBodyVisualsPatch 拦截 _Ready / PlayAnimation（否则 MegaSprite 校验会炸）。
    // 能量计数器 / 卡牌轨迹（CustomEnergyCounterPath / CustomTrailPath）
    // 还没有专属美术，继续沿用 PlaceholderID = ironclad 的占位场景。
    public override string CustomVisualPath => MyCharAssets.Scene("creature_visuals");

    public override string CustomCharacterSelectBg => MyCharAssets.Scene("select_bg");

    public override string CustomRestSiteAnimPath => MyCharAssets.Scene("rest_site");

    public override string CustomMerchantAnimPath => MyCharAssets.Scene("merchant");

    /// <summary>
    /// 顶部栏（左上角）角色头像。注意这里是**场景**，不是贴图：
    /// 引擎取的 <c>CharacterModel.Icon</c> = <c>ui/character_icons/&lt;id&gt;_icon</c>（PackedScene.Control），
    /// 在上面叠一个 TextureRect 装头像。只 override <see cref="CustomIconTexturePath"/> 换不掉左上角。
    /// </summary>
    public override string CustomIconPath => MyCharAssets.Scene("character_icon");

    public override string? CustomCharacterSelectIconPath => MyCharAssets.CharacterSelect;

    public override string? CustomCharacterSelectLockedIconPath => MyCharAssets.CharacterSelectLocked;

    /// <summary>顶部栏 / 遗物栏旁的角色头像。</summary>
    public override string? CustomIconTexturePath => MyCharAssets.CharacterHud;

    /// <summary>上面那张头像的白色剪影，顶部栏用它上色描边。</summary>
    public override string? CustomIconOutlineTexturePath => MyCharAssets.CharacterHudOutline;

    /// <summary>地图上的角色标记，复用角色头像。</summary>
    public override string? CustomMapMarkerPath => MyCharAssets.CharacterHud;

    /// <summary>地图画笔颜色，与卡框颜色保持一致。</summary>
    public override Color MapDrawingColor => new("c4d8f6");

    public override Color NameColor => new("D62000");

    public override CharacterGender Gender => CharacterGender.Neutral;

    public override int StartingHp => 75;

    public override int StartingGold => 99;

    public override CardPoolModel CardPool => ModelDb.CardPool<MyCharCardPool>();

    public override RelicPoolModel RelicPool => ModelDb.RelicPool<MyCharRelicPool>();

    public override PotionPoolModel PotionPool => ModelDb.PotionPool<MyCharPotionPool>();

    /// <summary>
    /// 起始牌组（见 docs/05 §6.3）：与其它角色一致的 4 打击 + 4 防御打底，
    /// 再各加一张核心牌：
    ///   1 × 阿尔法突袭 —— 响应+传参，打出后把池重新填满（自续）
    ///   1 × 参数激活 —— 点火牌，非响应、只提供传参
    /// 响应牌池空时打不出，所以点火牌是必须的；但它基础版不带「固有」，
    /// 第一回合不保证上手，靠打击/防御过渡，升级后才变成固有。
    /// </summary>
    public override IEnumerable<CardModel> StartingDeck => new CardModel[]
    {
        ModelDb.Card<MyCharStrike>(),
        ModelDb.Card<MyCharStrike>(),
        ModelDb.Card<MyCharStrike>(),
        ModelDb.Card<MyCharStrike>(),
        ModelDb.Card<MyCharDefend>(),
        ModelDb.Card<MyCharDefend>(),
        ModelDb.Card<MyCharDefend>(),
        ModelDb.Card<MyCharDefend>(),
        ModelDb.Card<AlphaStrike>(),
        ModelDb.Card<ParamActivation>(),
    };

    public override IReadOnlyList<RelicModel> StartingRelics => new RelicModel[]
    {
        ModelDb.Relic<MyCharStarterRelic>(),
    };

    /// <summary>
    /// 角色文本。BaseLib 会在 ModelDb.Init 时把这些 key 注入到 characters 表里，
    /// 所以不需要 pck / json 文件也能正常显示。
    /// key 由类名推导：MyChar -> characters.MY_CHAR.*
    ///
    /// 注意：这里只能提供"一种语言"。要多语言时，改为在 pck 里放
    /// localization/<lang>/characters.json（见 docs/03-本地化约定.md）。
    /// </summary>
    public override List<(string, string)>? Localization => new CharacterLoc(
        Title: "阿尔法",
        TitleObject: "阿尔法",
        Description: "一位再启动的人形，想要重新与世界建立链接。\n她将使用自己的训练方式，再次跟上时代。",
        PronounObject: "她",
        PronounSubject: "她",
        PronounPossessive: "她的",
        PossessiveAdjective: "她的",
        AromaPrinciple: "[sine][purple]参数到位，响应即至。[/purple][/sine]",
        EndTurnPingAlive: "[sine]继续迭代。[/sine]",
        EndTurnPingDead: "……",
        EventDeathPrevention: "核心尚未停机。",
        GoldMonologue: "[sine]还需要更多参数。[/sine]",
        CardsModifierTitle: "阿尔法卡牌",
        CardsModifierDescription: "奖励与商店中会出现阿尔法卡牌。",
        ExtraLoc: new[]
        {
            ("unlockText", "阿尔法已可供选择。"),
            ("bestiaryQuote", "参数到位，响应即至。"),
        });
}