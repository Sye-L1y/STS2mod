using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 起始「点火牌」：不带响应，随时可打，唯一作用是把传参池填上。
/// 基础版没有「固有」——第一回合不保证上手，靠 4 打击 / 4 防御过渡；
/// 升级后获得「固有」，开局必在手，点火更稳。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class ParamActivation : AlphaCard
{
    public override bool IsParamSource => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new AlphaBlockVar(7m, ValueProp.Move),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("param_activation");

    public override List<(string, string)>? Localization => new CardLoc(
        "参数激活",
        "获得{Block:diff()}点[gold]格挡[/gold]。\n[gold]传参[/gold]：抽一张牌。",
        ("flavor", "点火。第一条传参总得有人先写下去。"));

    public ParamActivation()
        : base(1, CardType.Skill, CardRarity.Basic, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        // ★ RawBlock()：与卡面预览（AlphaBlockVar）同口径；被「语义附加」授予响应后 α 协议增幅才生效。
        await CreatureCmd.GainBlock(Owner.Creature, RawBlock(), ValueProp.Move, cardPlay);
    }

    protected override ParamEntry? BuildParam() =>
        new() { Kind = ParamKind.Draw, Value = 1m, SourceId = "PARAM_ACTIVATION" };

    protected override void OnUpgrade()
    {
        DynamicVars.Block.UpgradeValueBy(2m);
        AddKeyword(CardKeyword.Innate);
    }
}