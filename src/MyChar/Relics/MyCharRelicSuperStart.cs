using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Relics;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 商店遗物「超级起步」：迭代间隔 ≥2 的牌（迭代2 / 迭代3），本场战斗**首次打出**时计数额外 +2。
/// 在 <c>BeforeCardPlayed</c> 里判「首次」——此时本牌的打出次数还没被记，为 0 即首次。
/// </summary>
[Pool(typeof(MyCharRelicPool))]
public sealed class MyCharRelicSuperStart : CustomRelicModel
{
    public override RelicRarity Rarity => RelicRarity.Shop;

    protected override string IconBaseName => "anchor";

    public override List<(string, string)>? Localization => new()
    {
        ("title", "超级起步"),
        ("description", "[gold]迭代[/gold]间隔≥2的牌，每场战斗首次打出时，本牌计数器额外+2。"),
        ("flavor", "起步就冲过头，后面只能更快。"),
    };

    public override Task BeforeCardPlayed(CardPlay cardPlay)
    {
        if (cardPlay.Card is not AlphaCard { IterationEvery: >= 2 } card)
        {
            return Task.CompletedTask;
        }
        if (card.Owner != Owner)
        {
            return Task.CompletedTask;
        }

        AlphaCorePower? core = AlphaCorePower.Of(Owner);
        if (core == null || core.GetPlayCount(card) != 0)
        {
            return Task.CompletedTask;
        }

        core.AddPlayCount(card, 2);
        Flash();
        return Task.CompletedTask;
    }
}