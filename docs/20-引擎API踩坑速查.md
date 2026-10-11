# 20 · 引擎 / API 踩坑速查（★ 改代码前必读）

> 从项目长期笔记（`.workbuddy/memory/MEMORY.md`）迁出，2026-10-11 整理。
> 这里沉淀的是**看过反编译源码 + 实测踩过的坑**，与具体卡牌无关；卡池权威清单见 [08](./08-卡牌清单.md)。
> 标注规则：★ = 踩过坑 / 反直觉。

---

## 1. 角色外观 / 贴图

- **头像 vs 贴图**：`CharacterModel.Icon` 取的是**场景** `ui/character_icons/<id>_icon`（左上角头像走这条，`NTopBarPortrait` 在其上叠 `TextureRect`）；`IconTexturePath` 只是贴图，用于图鉴 / 古人对白 / 多人房间。**换左上角头像必须提供自定义场景 + override `CustomIconPath`**（map 到 `res://MyChar/scenes/*.tscn`）。
- ★ **自定贴图路径必须真实存在**：BaseLib 立绘补丁是 `ResourceLoader.Load<Texture2D>(path).ResourcePath`，路径不存在 → `null` → NRE，且发生在 `ModelDb.Preload` 阶段，**会连带拖垮其它资源预载**（表现为「全部资源为空」）。
- ★ **新增卡牌必须同步补图**：`CustomPortraitPath`（`MyCharAssets.Card`）/ `CustomPackedIconPath`（`MyCharAssets.Power`）指向的 png 缺一个就 NRE。加卡 / 加 Power → 同步往 `make_alpha_cards.py` / `make_power_icons.py` 加 JOBS 并重跑。

## 2. Power 叠层（★ 按「触发条件」判定，2026-10-08 定稿）

- `PowerModel.InstanceType` 默认 `None`；`PowerCmd.Apply` 对已存在实例走 `ModifyAmount(+amount)`（**累加 Amount**，不抛）；`Instanced` 时 `FindExistingInstanceForStacking` 恒返 `null` → 每次 Apply 都新建。
- `NPower` 只给 `PowerStackType.Counter` 画数字（取 `DisplayAmount`，可 override）；`Single` = 一条栏、无数字。
- ★ **自定义 Power 必须自己读份数 / 累加值才有真叠加**；可用 `IAddDumbVariablesToPowerDescription` 把数值注入悬浮说明。
- **判定顺序**：触发条件是否一致 → 一致就合并、数值**相加**；不同就分实例。

| 情形 | 方案 |
| --- | --- |
| ① 回合态标记 / 一次性开关（如「封锁」`NoAttackPower`，回合末自毁） | `Single` 不叠 |
| ② **触发条件一致**、有值、无内部进度计数 | `Counter`，`Configure` 里 `DynamicVars[...].BaseValue += x` **累加**（★ 不是 ×份数、不是后写覆盖）。例：厄难覆盖 3+5=8；终响激发 失力 1+2=3、灾厄 5+7=12。★ 这类 `CanonicalVars` 初值必须写 **0**（该值已是「各份之和」的累加器） |
| ③ **触发条件不同**（升级前后门槛 / 阈值不同）但效果一致 | `Instanced` + **卡牌侧按门槛归并**：同门槛 `SetAmount(Amount + 1)` 加在同一个实例上，门槛不同则各占一条状态栏；降费 / 效果额度 = 该实例份数。目前仅「优美公式」（门槛 3 / 2）。★ **必须用 `Creature.GetPowerInstances<T>()` 找载体**（`GetPower<T>()` 只返回第一个，会写到别的门槛身上） |
| ④ **有内部进度计数**（打出后自己计数：高阶洞察 / 甲胄铸造 / 人格训练） | `Instanced`，`StackType` 回 `Single`，`PowerCmd.Apply<T>` 的返回值写参数 |

- ★ `Counter` **不会自动掉层**：只有本体 Weak / Vulnerable / Frail 自己在 `AfterSideTurnEnd` 调 `PowerCmd.TickDownDuration(this)`。
- ★ `ModelDb.Init` 对 `AllAbstractModelSubtypes` 逐个 `Activator.CreateInstance` → 新能力牌**无需手工注册**（`ModelId` = 类名 slug），但也**不能**用 abstract 中间基类当载体。

## 3. 本地化 / 卡面文本

- **卡面「升级前后不同」写法**：`{IfUpgraded:show:升级文案|基础文案}`（`ShowIfUpgradedFormatter`；`CardModel.GetDescriptionForPile` 恒注入 `IfUpgradedVar`，不依赖 SimpleLoc）。升级才出现的文本写 `{IfUpgraded:show:+1|}`。
- ★ **本项目未启用 SimpleLoc**（无 `EnableSimpleLoc("MyChar")`）：简写 `*gold*` / `$blue$` / `-基础-+升级+` **一律不生效**，只能写展开形式 `[gold]…[/gold]`、`{IfUpgraded:...}`。
- ★ **卡面数值要随升级变绿**：升级会变的数值**必须做成 `DynamicVar`**（`{X:diff()}`），写死 const 不会更新描述。

## 4. 关键词

- ★ **「升级后才出现的原版关键词」（固有等）= ① 不写 `BaseKeywords`（或删掉覆写）+ ② `OnUpgrade() => AddKeyword(CardKeyword.Innate)`，两步缺一不可**。
  - 只做 ② → 退化成「本来就自带」；只做 ① → 升级也不给。
  - 原因：`CardModel.LocalKeywords` 会在升级前把关键词集合**缓存死**（`BaseKeywords => IsUpgraded ? ... : ...` 这种写法升级后不刷新）。
  - 先例：持续协议、聚神进化、流水线 / 无限注入、冷息（v1.14 补上漏删的 `BaseKeywords`）。
- **卡面描述不重复写关键词**：`CardKeyword.Innate`（固有）等由 `BaseKeywords` 提供，chip + 悬浮自动生成，描述里不再写「（固有）」（同完全形态的「虚无」口径）。

## 5. 传参 / 响应语义（规则 P9–P12）

- 传参入池后要等**响应牌结算**才真正生效（不是打出即生效）。
- ★ 传参加伤害 / 加效果**一律只在「响应牌本次结算」生效**，**不写回卡牌、不留存**。
  - 例：战斗预设 = 本牌**本次**打出的伤害值；攻守兼备 = 响应牌**本次**造成伤害时获得等量格挡；击打测试 = 响应牌**下一次**打出时回到手牌（仍是本次结算）。
- ★ **P10 传参默认一次性**：条目被消费即消失（池清空）；**只有「给本牌 / 手牌加关键词」类（`ParamKind.Keyword` / `KeywordAllHand`）才持久**（例：使卡牌获得重放 1 / 保留）——用户原话「传参本身词条已经说清楚了」，卡面不额外写文案。
- ★ **「回手」= `ParamKind.ReturnToHand`（v1.11，E1 的 回手 项）**：响应对照旧 `ApplyParam` 里只置标记 `_returnToHandAfterPlay`，打完在 **`AlphaCard.AfterCardPlayedLate`** 里 `cardPlay.Card == this` 且不在手牌 → `CardPileCmd.Add(this, PileType.Hand)`。一次性、不留在牌上。
  - ★ `AfterCardPlayedLate` 是**广播**给 `CombatState.IterateHookListeners()`（所有 Power + 所有牌堆的牌），打出中的那张牌自己也会收到；本体 Bolas / MakeItSo / RightHandHand 都这么写，**别用 `BeforeHandDraw`**（那是「上回合打过的牌下回合抽牌前回手」）。
  - ★ **「回手」两种来源共用同一个落点**：① `_returnToHandAfterPlay`（回手传参）；② `AdjustmentPower` 额度（打出的迭代牌）。放在这里是因为 `AfterCardPlayed` 阶段牌可能还在出牌区、紧接着会被引擎移到弃牌堆，只有 Late 才是「已经不在手牌」的稳定时机。
- ★ **「预输入」（下一次响应不消耗传参）**：`AlphaCorePower.Data.NextResponseFree` + `Set/TakeNextResponseFree()`；`AlphaCard.OnPlay` 响应阶段 `ConsumesParams && core.HasParams && core.TakeNextResponseFree()`。★ **池空时不消耗额度**（否则空响应白吃 buff）。
- ★ **P11「重复上一次的传参」口径（v1.12，二次输入）**：= 把**最近一次入池批次**原样再入池一遍，等价于「重复上一次打出的带传参的牌所带的传参」。载体：`AlphaCorePower.Data.LastEntered`（每次 `AddParam` 追加）+ `BeginParamBatch()`（清空分段）+ `LastEnteredParams`（只读快照）。
  - ★ **每个「一次性抛多条」的入口都要先 `BeginParamBatch()`**：`AlphaCard` 步骤 3、`InfiniteInjectionPower`、`FullFormPower`（循环外）、`ProtocolCorePower`、`IterationBroadcastPower`、`SpecialParamPotion`。
- ★ **P12 运行时授予「响应」只本场有效、只对自家牌**：`AlphaCard.IsResponse` 是**天生**的 virtual；临时授予另走 `GrantResponse()` + `HasResponse => IsResponse || _grantedResponse`。
  - ★ **所有机制判断必须读 `HasResponse`**（`CanonicalKeywords` / `OnPlay` 步骤 1 / `PreviewDamageAddFor` / `PreviewBoostFor`），外部筛选点也别漏（`Call.IsLinked`、`DisparityCount.IsDisparate`）。
  - 授予只挂**手牌实例** → 战斗结束即失效、不写回牌组。先例：语义附加（E24）。

## 6. 一条牌多条传参

- `AlphaCard.BuildParams()`（`IEnumerable<ParamEntry>`，默认包裹 `BuildParam()`）；`OnPlay` 步骤 3 遍历入池。单条直接用 `BuildParam()` 即可（旧牌无需改）。
- ★ **`BuildParam` 里的取值时机**：`OnPlay` = 响应 → 本牌效果（`finally` 里已把 `_resolving` 还原 false）→ 传参 → 迭代。要在传参里带「本牌本次实际打出值」（如战斗预设）必须**在 `PlayEffect` 期间先快照**，`BuildParam` 时再取 `RawDamage()` 只会拿到预览值。
- ★ **惰性迭代器陷阱**：`BuildParams()` 是 `yield` 惰性求值。要在循环前清批次（`BeginParamBatch()`）时，**必须先 `.ToList()` 材质化**再清，否则循环里读到的是被清空后的集合。

## 7. 伤害 / 攻击

- ★ **多段攻击一律用单命令 `WithHitCount(n)`**（`DamageCmd.Attack(dmg).FromCard(this,cardPlay).TargetingXxx().WithHitCount(n).Execute(cc)`），**不要 `for` 循环发多次 `Attack`**：
  - 循环 = N 个独立攻击事件（`BeforeAttack` / `AfterAttack` 各触发 N 次）；
  - `VigorPower`（活力「下一次攻击」）在循环里**只有第一段**吃得到，`WithHitCount` 则每段都吃满；
  - `Hook.ModifyAttackHitCount` 只对 `WithHitCount` 生效。
  - 先例：旋风斩 / 阿尔法突袭 / 阿尔法锚点 / 协议提取 / 协议结算 / 多层网络输出。
- ★ **X=0 时不要发命令**（否则空走 `BeforeAttack` / `AfterAttack` 白扣活力）。用 `if (times > 0)` 守卫。
- **`DamageResult` 字段**：`TotalDamage` = `BlockedDamage` + `UnblockedDamage`（「造成的伤害」用它）；`UnblockedDamage` 是穿盾后的（目标回馈用）；`WasTargetKilled` 判击杀（多段攻击里判「本牌是否击杀」就用它）。
- ★ **「伤害转格挡」`ParamKind.BlockFromDamage`（v1.12，攻守兼备）**：`ApplyParam` 只置标记 `_gainBlockFromDamage`；`AlphaCard.AfterDamageGiven` 里对 `_resolving && 标记 && cardSource == this` 的伤害累加 `result.TotalDamage`；`OnPlay` 步骤 **2.6** 一次性 `CreatureCmd.GainBlock`，没伤害就不给。
  - ★ 能成立是因为**打出的牌在结算期间住在 `PileType.Play` 堆**，而 `PlayerCombatState.AllPiles = {Hand,Draw,Discard,Exhaust,Play}` → `CombatState.IterateHookListeners()` 会把**牌堆里的卡牌**也纳入监听者。
- ★ **「技能牌没有敌方目标」时随机挑敌人**：`AlphaCard.RandomEnemy()`（已从 `private` 改 `protected`，供异常解析用）。

## 8. 回合流程 / 战斗结束

- **回合开始的「每回合额外抽 X」**：`AfterPlayerTurnStart` 里 `CardPileCmd.Draw`（与「待发抽牌」同款）。延迟型（下回合）也用它。
- ★ **回合末顺序（写回合末效果的牌必看）**：`CombatManager.EndPlayerTurnPhaseOneInternal` 派发 `Hook.BeforeSideTurnEnd` →（PhaseTwo）`FlushPlayerHand` 才按 `CardModel.ShouldRetainThisTurn` 分流弃牌。
  - **所以只有 `BeforeSideTurnEnd`（或更早）能赶在弃牌前挂 `GiveSingleTurnRetain()`**（冷息）；`AfterSideTurnEnd` 已经晚了。单回合标记由 `CardModel.EndOfTurnCleanup` 自动清。
- ★ **战斗结束顺序（写战后效果的牌必看）**：`EndCombatInternal` 先 `Hook.AfterCombatEnd(runState, combatState, room)`，**之后**才 `player.AfterCombatEnd()` 清掉身上的 Power。
  - **所以 Power 能在 `AfterCombatEnd` 里兑现战后奖励**（多层网络输出：`player.Deck.Cards.Where(IsUpgradable)` + `player.RunState.Rng.Niche.NextItem` + `CardCmd.Upgrade`；随机源同本体 / BaseLib `RandomCardUpgradeReward`）。
- ★ **手牌上限钩子 `IMaxHandSizeModifier`**（BaseLib.Hooks）：Power 实现它即可生效，**无需注册**。链路：`MaxHandSizePatch` 遍历 `RunState.IterateHookListeners(combatState)` → childCombatState != null 时继续 `CombatState.IterateHookListeners()` → `list.AddRange(creature.Powers)`。（别只看 RunState 那段的前半，前半只加 Deck 的牌 + ModHelper 订阅者；Power 是从「继续调 CombatState 那步」进来的。）
- **每回合「一次」额度型 Power（v1.13，调节）**：`AdjustmentPower`（`Single`）用私有 `bool _usedThisTurn` + 静态 `TryConsumeTurnSlot(player)`（内部 `player.Creature.GetPower<T>()`）；`AfterPlayerTurnStart` 里判断 `player.Creature != Owner` 后刷新。
  - ★ 状态变化后调 `InvokeDisplayAmountChanged()`，状态栏文案（`IAddDumbVariablesToPowerDescription` 注入的 `{State}`）才会刷新。

## 9. 美术 / 场景 / 资源管线

- 尺寸约定、`.tscn` 手写与追加、`RelicImageOverridePatch`、能量图标、药水 override：见 [02](./02-美术资源规格与接入.md)。
- **战损立绘机制（v1.16）**：`Creature.CurrentHpChanged`（`event Action<int,int>?`，old/new）→ 在 `NCreature._Ready` Postfix 订阅，按 `CurrentHp / MaxHp ≤ DamagedThreshold` 切换 `%Visuals` ↔ `%DamagedSprite` 可见性；`_ExitTree` Postfix 退订（用 `GetInstanceId()` 字典跟踪）。档位结构预留（改常量 → 素材表即可）。见 `src/MyChar/Patches/DamageVisualsPatch.cs`。
- ★ **pck 追加格式**：目录条目路径字段 `plen = align4(路径字节)`，补齐 `\x00` **算在 plen 内**。写错 → 游戏里「所有资源为空 + 立绘预加载 NRE」，但编译 / pck 生成都不报错。改完必须 `python tools/pck_verify.py <pck>`（`build.ps1` 已自动调用并拦截）。
- **卡图占位图机制（★ 卡图错配的根因）**：`tools/make_alpha_cards.py` 生成的 500×380 占位图**把卡名画在图上**（底部居中，微软雅黑 40px；顶部 PLACEHOLDER；底=竖向渐变 + 主色顶带 + 中央菱形）。所以「某卡借用了别的卡的 slug」→ 游戏里卡面写着**别人的卡名**。改卡名 / 加卡后**必须往 JOBS 加条目并重跑**。
- **`tools/check_card_art.py`（加卡 / 改名后必跑）**：解析 `make_alpha_cards.py` 的 JOBS（slug → 图上写的卡名）与各卡 `.cs` 的 `CustomPortraitPath` + `CardLoc` 卡名，三者对齐才算过；顺带报「引用了不存在的图」。★ **只比对「卡名」，查不出「配色与卡牌类型不符」** → JOBS 参数选对类型色（ATTACK 红 / SKILL 蓝 / POWER 紫 / PROTO 青）。真美术例外：`defend` / `strike`。

## 10. 工具 / 环境

- **Bash 工具不可靠**（Git Bash shim 损坏：`dirname` / `cd` / `ls` not found，exit 127）；**PowerShell 工具 stdout 不回传** → 一律 `... | Out-File -Encoding utf8 xxx.txt` 落盘再 `Read`。
- **Python 打印中文到 stdout 会乱码**（PowerShell 下）；写文件用 `io.open(..., encoding="utf-8").write`。
- **图片用 `Read` 工具看不了**（当前模型不支持图像）→ 靠 PIL 生成的拼版图 + 数值探测（alpha 四角采样）确认产物。
- ★ **美术脚本需要 PIL**：托管 Python 3.13.12 没有 PIL → 用 `E:\Anaconda\python.exe`（3.11.5）。`build.ps1` 里的 `python` 只跑 pck_append / pck_verify，不依赖 PIL。
- ★ **同一个文件的多个 Edit 不能并行**（互相覆盖、偶发 EBUSY）。不同文件并行安全；同文件串行，或 `Read` + `Write` 整文件。
- ★ bash 里**不能** `powershell -File ...`（被安全策略拦截）→ PowerShell 脚本只能用 PowerShell 工具跑。游戏日志在 `%APPDATA%\SlayTheSpire2\logs\godot.log`。
- **定位编译错误最快**：`dotnet build src/MyChar/MyChar.csproj -c Release -v minimal` 看尾部 30 行。
- git 开 `core.autocrlf=true`；`.tscn` 按原始字节打进 pck，`.gitattributes` 锁 `*.tscn text eol=lf`。
- ★ **AI 环境内无法 `git push` / `git fetch`**（2026-10-11 实测）：工具进程带 `HTTP_PROXY=HTTPS_PROXY=http://127.0.0.1:57431`（环境注入的出口代理），该代理对 `github.com` 的 CONNECT 返回 **`502 CONNECT tunnel failed`**；清空代理走直连则 **443 无路由**。→ **提交/整理可在环境内完成，`git push` 必须由用户在本机终端执行**。
- ★ 本环境**删除文件会走回收站包装器**（`safe-delete`），`Remove-Item` 常报错退出码 1，但文件实际已移入回收站 → 批量删除要 `try { Remove-Item -LiteralPath $t -Force -ErrorAction Stop } catch {}` 逐项包住再回读校验。
- 远端：`https://github.com/Sye-L1y/STS2mod.git`（分支 `master`）。
