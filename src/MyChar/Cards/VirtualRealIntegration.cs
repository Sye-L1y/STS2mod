using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 稀有技能「虚实结合」：消耗；失去 9 点生命，获得 1 层[gold]无实体[/gold]（升级后失血降为 6，并获得[gold]保留[/gold]）；
/// [gold]传参[/gold]：给所有手牌添加[gold]虚无[/gold]。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class VirtualRealIntegration : AlphaCard
{
    public override bool IsParamSource => true;

    protected override IEnumerable<CardKeyword> BaseKeywords =>
        new[] { CardKeyword.Exhaust };

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new HpLossVar(9m),
        new DynamicVar("Intangible", 1m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("virtual_real_integration");

    public override List<(string, string)>? Localization => new CardLoc(
        "虚实结合",
        "失去{HpLoss:diff()}点生命，获得{Intangible:diff()}层[gold]无实体[/gold]。\n[gold]传参[/gold]：给所有手牌添加[gold]虚无[/gold]。",
        ("flavor", "把自己折进代码的缝里，实体就伤不到你。"),
        ("selectionScreenPrompt", "选择 {Amount} 张牌加入手牌"));

    public VirtualRealIntegration()
        : base(1, CardType.Skill, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await CreatureCmd.Damage(choiceContext, Owner.Creature, DynamicVars.HpLoss.BaseValue,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move, this, cardPlay);
        await PowerCmd.Apply<IntangiblePower>(choiceContext, Owner.Creature,
            ResolveValue("Intangible"), Owner.Creature, this);
    }

    protected override ParamEntry? BuildParam() =>
        new()
        {
            Kind = ParamKind.KeywordAllHand,
            Value = 1m,
            Variant = (int)CardKeyword.Ethereal,
            SourceId = "VIRTUAL_REAL_INTEGRATION",
        };

    // 保留走 AddKeyword：BaseKeywords 依赖 IsUpgraded 时，LocalKeywords 会在升级前就被缓存，升级后不会刷新。
    protected override void OnUpgrade()
    {
        DynamicVars.HpLoss.UpgradeValueBy(-3m);
        AddKeyword(CardKeyword.Retain);
    }
}