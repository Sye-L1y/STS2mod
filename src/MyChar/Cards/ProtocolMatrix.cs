using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 稀有能力「协议矩阵」：
///   1. 本场战斗中所有 α/β/γ 协议牌升级（已生成的立即升级，之后生成的自动升级）；
///   2. 本场战斗首次累计获得 50 层协议时，对所有敌人造成一次伤害——
///      伤害 = 当前消耗堆中每张 α/β/γ 协议牌各自提供的数值之和。
///
/// 每张协议牌提供多少伤害只由**本牌**是否升级决定（基础 5/10/15，升级后 8/15/22）；
/// 协议牌自身是否升级不影响该数值。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class ProtocolMatrix : AlphaCard
{
    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("AlphaDamage", 5m),
        new DynamicVar("BetaDamage", 10m),
        new DynamicVar("GammaDamage", 15m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("protocol_resonance");

    public override List<(string, string)>? Localization => new CardLoc(
        "协议矩阵",
        "本场战斗中所有[gold]α/β/γ协议[/gold]牌升级。本场战斗首次累计获得50层[gold]协议[/gold]时，对所有敌人造成一次伤害：当前消耗堆中每张α/β/γ协议牌分别提供{AlphaDamage:diff()}/{BetaDamage:diff()}/{GammaDamage:diff()}点伤害。",
        ("flavor", "把整套协议重写一遍，顺便留一个总闸。"));

    public ProtocolMatrix()
        : base(2, CardType.Power, CardRarity.Rare, TargetType.Self)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        await PowerCmd.Apply<ProtocolMatrixPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);

        // 每张协议牌提供多少伤害由本牌（是否升级）决定，写进 Power 供里程碑结算读取。
        ProtocolMatrixPower? power = Owner.Creature.GetPower<ProtocolMatrixPower>();
        power?.Configure(
            DynamicVars["AlphaDamage"].IntValue,
            DynamicVars["BetaDamage"].IntValue,
            DynamicVars["GammaDamage"].IntValue);

        // 已经在场的协议牌立即升级；之后生成的由 ProtocolMatrixPower.UpgradeNewProtocol 兜底。
        foreach (CardModel card in CardPile.GetCards(Owner,
            PileType.Hand, PileType.Draw, PileType.Discard, PileType.Exhaust))
        {
            if (ProtocolMatrixPower.IsProtocolCard(card) && !card.IsUpgraded)
            {
                CardCmd.Upgrade(card);
            }
        }
    }

    protected override void OnUpgrade()
    {
        DynamicVars["AlphaDamage"].UpgradeValueBy(3m);
        DynamicVars["BetaDamage"].UpgradeValueBy(5m);
        DynamicVars["GammaDamage"].UpgradeValueBy(7m);
    }
}