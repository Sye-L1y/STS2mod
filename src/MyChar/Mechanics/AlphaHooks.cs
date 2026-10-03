using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace MyCharMod.Mechanics;

/// <summary>「响应牌完成一次响应」的监听者（E2）。实现在自定义 Power 或遗物上。</summary>
public interface IAlphaResponseListener
{
    Task OnAlphaResponse(PlayerChoiceContext choiceContext, CardModel card);
}

/// <summary>
/// 「别处触发了响应」时做出反应的**卡牌**（如「零基启动」触发响应时回手）。
/// 卡牌不是玩家身上的 Power / 遗物，走不了 <see cref="AlphaHooks.Listeners"/>，
/// 所以在 <see cref="AlphaHooks.ResponseTriggered"/> 里额外扫一遍玩家的抽牌堆 / 弃牌堆。
/// </summary>
public interface IAlphaResponseReactor
{
    Task OnResponseTriggered(PlayerChoiceContext choiceContext, Player player);
}

/// <summary>「某张迭代牌完成一次迭代（层数 +1）」的监听者（E3）。实现在自定义 Power 或遗物上。</summary>
public interface IAlphaIterationListener
{
    Task OnAlphaIteration(PlayerChoiceContext choiceContext, CardModel card);
}

/// <summary>「一张 α/β/γ 协议牌被打出」的监听者（E9）。供「欧米伽协议」遗物监听。</summary>
public interface IAlphaProtocolListener
{
    Task OnAlphaProtocol(PlayerChoiceContext choiceContext, CardModel card);
}

/// <summary>
/// 起始遗物（普通版 / 古老版）实现它，用来告诉核心层「每满 3 次响应/迭代给几层协议」，
/// 并让遗物在计数变化时刷新计数条。
/// </summary>
public interface IAlphaProtocolMilestone
{
    /// <summary>每满 3 次响应/迭代发放的协议层数（普通 1 / 古老 2）。</summary>
    int ProtocolPerMilestone { get; }

    /// <summary>响应/迭代计数变化时刷新遗物计数条。</summary>
    void NotifyCounterChanged();
}

/// <summary>
/// 核心机制的事件广播（E2 / E3 / E9）。
///
/// 不走 C# 事件订阅——Power / 遗物的挂载、移除时机不受我们控制，漏订阅或漏退订都会出问题。
/// 改成「打出时遍历玩家身上的监听者，谁实现了监听接口就回调谁」：无需生命周期管理，
/// 且天然只影响本场战斗内、当前玩家身上的 Power 与遗物。
/// </summary>
internal static class AlphaHooks
{
    public static async Task ResponseTriggered(PlayerChoiceContext choiceContext, CardModel card)
    {
        foreach (object listener in Listeners(card))
        {
            if (listener is IAlphaResponseListener response)
            {
                await response.OnAlphaResponse(choiceContext, card);
            }
        }

        // 卡牌级反应者（零基启动等）：响应触发时，把自己从抽牌堆 / 弃牌堆捞回手牌。
        if (card.Owner is Player player)
        {
            foreach (CardModel reactor in ReactorCards(player))
            {
                if (reactor is IAlphaResponseReactor responseReactor)
                {
                    await responseReactor.OnResponseTriggered(choiceContext, player);
                }
            }
        }
    }

    /// <summary>玩家抽牌堆 / 弃牌堆里实现了 <see cref="IAlphaResponseReactor"/> 的牌（消耗堆里的不捞）。</summary>
    private static List<CardModel> ReactorCards(Player player) =>
        CardPile.GetCards(player, PileType.Draw, PileType.Discard)
            .Where(candidate => candidate is IAlphaResponseReactor)
            .ToList();

    public static async Task IterationCompleted(PlayerChoiceContext choiceContext, CardModel card)
    {
        foreach (object listener in Listeners(card))
        {
            if (listener is IAlphaIterationListener iteration)
            {
                await iteration.OnAlphaIteration(choiceContext, card);
            }
        }
    }

    public static async Task ProtocolTriggered(PlayerChoiceContext choiceContext, CardModel card)
    {
        foreach (object listener in Listeners(card))
        {
            if (listener is IAlphaProtocolListener protocol)
            {
                await protocol.OnAlphaProtocol(choiceContext, card);
            }
        }
    }

    /// <summary>
    /// 监听者 = 玩家身上的 Power ∪ 玩家身上的遗物（E6）。
    /// 先拷贝一份再遍历：回调里可能会挂载 / 移除 Power，直接遍历会改集合。
    /// </summary>
    private static List<object> Listeners(CardModel card)
    {
        if (!card.IsMutable || card.Owner is not Player player)
        {
            return new List<object>();
        }

        var listeners = new List<object>();
        Creature? creature = player.Creature;
        if (creature != null)
        {
            listeners.AddRange(creature.Powers);
        }
        listeners.AddRange(player.Relics);
        return listeners;
    }
}