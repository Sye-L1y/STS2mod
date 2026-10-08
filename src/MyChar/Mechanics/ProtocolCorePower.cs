using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace MyCharMod.Mechanics;

/// <summary>
/// 「协议核心」的载体：打出 α/β/γ 协议牌时，往传参池里塞一条「生成同款协议牌」的传参，
/// 由下一张响应牌消费时再生一张。
/// </summary>
public sealed class ProtocolCorePower : CustomPowerModel, IAlphaProtocolListener
{
    public override PowerType Type => PowerType.Buff;

    public override PowerStackType StackType => PowerStackType.Single;

    public override bool ShouldScaleInMultiplayer => false;

    public override string? CustomPackedIconPath => MyCharAssets.Power("protocol");

    public override string? CustomBigIconPath => MyCharAssets.Power("protocol");

    public override List<(string, string)>? Localization => new()
    {
        ("title", "协议核心"),
        ("description", "打出[gold]α/β/γ协议[/gold]牌时，[gold]传参[/gold]：生成一张同款协议牌（生成的牌不再触发此效果）。"),
    };

    public async Task OnAlphaProtocol(PlayerChoiceContext choiceContext, CardModel card)
    {
        // 由传参生成的协议牌不再触发协议核心，避免「打出协议 → 传参生成同款 → 再打出」无限连锁（永续）。
        if (card is AlphaCard { GeneratedByParam: true })
        {
            return;
        }

        int variant = card switch
        {
            ProtocolAlpha => (int)GenCardKind.ProtocolAlpha,
            ProtocolBeta => (int)GenCardKind.ProtocolBeta,
            ProtocolGamma => (int)GenCardKind.ProtocolGamma,
            _ => -1,
        };
        if (variant < 0)
        {
            return;
        }

        AlphaCorePower? core = Owner.Player == null ? null : AlphaCorePower.Of(Owner.Player);
        if (core != null)
        {
            await core.AddParamWithHook(choiceContext, new ParamEntry
            {
                Kind = ParamKind.GenerateCard,
                Value = 1m,
                Variant = variant,
                SourceId = "PROTOCOL_CORE",
            });
        }
    }
}