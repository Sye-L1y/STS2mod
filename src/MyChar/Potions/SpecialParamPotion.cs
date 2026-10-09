using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 稀有专属药水「特别参数」：获得 10 格挡，并向传参池写入一条
/// 「从 3 张本职业稀有牌中选 1 张免费打出并消耗」的传参（<see cref="ParamKind.FreePlayRareCard"/>）。
/// 格挡立即生效；传参由下一张响应牌消费时结算。
/// </summary>
[Pool(typeof(MyCharPotionPool))]
public sealed class SpecialParamPotion : CustomPotionModel
{
    public override PotionRarity Rarity => PotionRarity.Rare;

    public override PotionUsage Usage => PotionUsage.CombatOnly;

    public override TargetType TargetType => TargetType.AnyPlayer;

    public override string? CustomPackedImagePath => MyCharAssets.Potion("special_param");

    public override string? CustomLargeImagePath => MyCharAssets.Potion("special_param");

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new BlockVar(10m, ValueProp.Unpowered),
    };

    public override List<(string, string)>? Localization => new PotionLoc(
        "特别参数",
        "获得{Block}点[gold]格挡[/gold]。\n[gold]传参[/gold]：从3张本职业稀有牌中选择1张免费打出并消耗。");

    protected override async Task OnUse(PlayerChoiceContext choiceContext, Creature? target)
    {
        PotionModel.AssertValidForTargetedPotion(target);
        await CreatureCmd.GainBlock(Owner.Creature, DynamicVars.Block, null);

        AlphaCorePower? core = AlphaCorePower.Of(Owner);
        if (core != null)
        {
            core.BeginParamBatch();
            await core.AddParamWithHook(choiceContext, new ParamEntry
            {
                Kind = ParamKind.FreePlayRareCard,
                Value = 1m,
                SourceId = "SPECIAL_PARAM",
            });
        }
    }
}