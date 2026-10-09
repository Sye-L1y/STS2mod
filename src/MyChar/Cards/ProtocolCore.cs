using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 稀有能力「协议核心」：[gold]虚无[/gold]（升级去除）；
/// 打出 α/β/γ 协议牌时，[gold]传参[/gold]：生成一张同款协议牌。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class ProtocolCore : AlphaCard
{
    protected override IEnumerable<CardKeyword> BaseKeywords => new[] { CardKeyword.Ethereal };

    public override string? CustomPortraitPath => MyCharAssets.Card("protocol_core");

    public override List<(string, string)>? Localization => new CardLoc(
        "协议核心",
        "打出[gold]α/β/γ协议[/gold]牌时，[gold]传参[/gold]：生成一张同款协议牌（生成的牌不再触发此效果）。",
        ("flavor", "每一次调用都留一份底稿，随时可以再放一次。"));

    public ProtocolCore()
        : base(3, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<ProtocolCorePower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade() => RemoveKeyword(CardKeyword.Ethereal);
}