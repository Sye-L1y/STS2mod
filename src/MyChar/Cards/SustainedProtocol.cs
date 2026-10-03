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

/// <summary>稀有能力：常驻的协议来源，每回合结束自动进账 3 层。升级后获得「固有」（开局必在手）。</summary>
[Pool(typeof(MyCharCardPool))]
public sealed class SustainedProtocol : AlphaCard
{
    public override bool GrantsProtocol => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Protocol", 3m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("sustained_protocol");

    public override List<(string, string)>? Localization => new CardLoc(
        "持续协议",
        "每回合结束时获得{Protocol:diff()}层[gold]协议[/gold]。",
        ("flavor", "常驻的授权，不会随回合消失。"));

    public SustainedProtocol()
        : base(1, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<SustainedProtocolPower>(choiceContext, Owner.Creature,
            DynamicVars["Protocol"].BaseValue, Owner.Creature, this);
    }

    // 固有走 AddKeyword：BaseKeywords 依赖 IsUpgraded 时，LocalKeywords 会在升级前就被缓存成不含固有的集合，
    // 升级后不会刷新（见 CardModel.LocalKeywords）。
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Innate);
}