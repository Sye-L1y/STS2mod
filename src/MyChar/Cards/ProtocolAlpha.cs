using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>协议牌：回合开始时由「协议」层数生成，0 费、消耗、虚无。衍生牌，挂在专属池（非角色池）。</summary>
[Pool(typeof(MyCharTokenCardPool))]
public sealed class ProtocolAlpha : AlphaCard
{
    public override bool IsParamSource => true;

    protected override IEnumerable<CardKeyword> BaseKeywords =>
        new[] { CardKeyword.Exhaust, CardKeyword.Ethereal };

    /// <summary>增幅按百分比存（50 = +50%），升级 +50 → 100。</summary>
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Boost", 50m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("protocol_alpha");

    public override List<(string, string)>? Localization => new CardLoc(
        "α协议",
        "获得1点[gold]能量[/gold]。\n[gold]传参[/gold]：下一张响应牌的最终伤害与格挡+{Boost:diff()}%（按迭代成长后的数值计算）。",
        ("flavor", "最低权限，也最廉价。"));

    public ProtocolAlpha()
        : base(0, CardType.Skill, CardRarity.Token, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PlayerCmd.GainEnergy(1m, Owner);
        // E9：α/β/γ 协议牌被打出 → 广播（「欧米伽协议」遗物挂在这上面）。
        await AlphaHooks.ProtocolTriggered(choiceContext, this);
    }

    protected override ParamEntry? BuildParam() =>
        new() { Kind = ParamKind.EffectBoost, Value = DynamicVars["Boost"].BaseValue / 100m, SourceId = "PROTOCOL_ALPHA" };

    protected override void OnUpgrade()
    {
        DynamicVars["Boost"].UpgradeValueBy(50m);
    }
}