using System.Collections.Generic;
using System.Threading.Tasks;
using BaseLib.Abstracts;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MyCharMod.Mechanics;

namespace MyCharMod;

/// <summary>
/// 普通技能「广播指令」：自身无即时效果；[gold]传参[/gold]：给所有敌人叠 2（3）层[gold]易伤[/gold]一次。
/// </summary>
[Pool(typeof(MyCharCardPool))]
public sealed class BroadcastCommand : AlphaCard
{
    public override bool IsParamSource => true;

    protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
    {
        new DynamicVar("Stacks", 2m),
    };

    public override string? CustomPortraitPath => MyCharAssets.Card("broadcast_command");

    public override List<(string, string)>? Localization => new CardLoc(
        "广播指令",
        "[gold]传参[/gold]：给所有敌人叠{Stacks:diff()}层[gold]易伤[/gold]一次。",
        ("flavor", "一条广播，所有终端同时收到。"));

    public BroadcastCommand()
        : base(1, CardType.Skill, CardRarity.Common, TargetType.Self)
    {
    }

    protected override Task PlayEffect(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
        Task.CompletedTask;

    protected override ParamEntry? BuildParam() =>
        new()
        {
            Kind = ParamKind.DebuffAll,
            Value = DynamicVars["Stacks"].BaseValue,
            Variant = (int)DebuffKind.Vulnerable,
            SourceId = "BROADCAST_COMMAND",
        };

    protected override void OnUpgrade() => DynamicVars["Stacks"].UpgradeValueBy(1m);
}