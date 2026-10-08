using System;
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
/// 普通技能「格挡置换」：去除一个敌人的所有[gold]格挡[/gold]；
/// [gold]传参[/gold]：获得本牌去除的等量[gold]格挡[/gold]。升级后获得[gold]保留[/gold]。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class BlockDisplacement : AlphaCard
{
    public override bool IsParamSource => true;

    /// <summary>本牌本次去除的敌人格挡量，供 <see cref="BuildParam"/> 写入传参。</summary>
    private decimal _removedBlock;

    public override string? CustomPortraitPath => MyCharAssets.Card("protocol_bulwark");

    public override List<(string, string)>? Localization => new CardLoc(
        "格挡置换",
        "去除一个敌人的所有[gold]格挡[/gold]。\n[gold]传参[/gold]：获得本牌去除的等量[gold]格挡[/gold]。",
        ("flavor", "把对方的护盾拆下来，装到自己身上。"));

    public BlockDisplacement()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.AnyEnemy)
    {
    }

    protected override async Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
        _removedBlock = 0m;

        decimal block = cardPlay.Target.Block;
        if (block <= 0)
        {
            return;
        }
        await CreatureCmd.LoseBlock(choiceContext, cardPlay.Target, block, Owner.Creature);
        _removedBlock = block;
    }

    protected override ParamEntry? BuildParam() =>
        _removedBlock > 0
            ? new ParamEntry
            {
                Kind = ParamKind.GainBlock,
                Value = _removedBlock,
                SourceId = "BLOCK_DISPLACEMENT",
            }
            : null;

    /// <summary>升级后才获得「保留」，走 AddKeyword 而非 BaseKeywords，避免关键词集合缓存不刷新。</summary>
    protected override void OnUpgrade() => AddKeyword(CardKeyword.Retain);
}