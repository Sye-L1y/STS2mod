# 阿尔法 · 杀戮尖塔 2 新角色 mod

新增可玩角色「阿尔法」，含独立卡池、起始卡组、遗物池、药水池与专属美术。

定位：**蓄力 → 连锁 → 爆发**的成长型角色，三大核心机制——

- **传参 / 响应**：传参牌往池里塞效果，响应牌打出时消费整池结算（软门禁，池空也能打）；
- **协议**：层数计数器，回合开始按档位发 α（1-4）/ β（5-12）/ γ（13+）协议牌，超 13 层溢出转活力；
- **迭代**：每打出 X 次叠一层效果，本场战斗永久成长，卡面数值实时反映。

依赖 [BaseLib](https://github.com/Alchyr/BaseLib) ≥ 3.4.5。

## 快速开始

```powershell
# 编译 + 部署到游戏（重启游戏生效）
.\tools\build.ps1 -Deploy
```

游戏里选人界面就会出现「阿尔法」。

## 目录结构

```
src/MyChar/                  C# 源码
    MyChar.csproj            项目 + 游戏/BaseLib 引用 + pck 打包
    ModEntry.cs              入口，注册遗物图标
    MyCharAssets.cs          资源路径统一入口
    Character/               角色本体 + 卡池/遗物池/药水池
    Cards/                   卡牌
    Relics/                  遗物
    Mechanics/               机制类（传参池/协议/迭代）
    Patches/                 Harmony 补丁（含静态立绘支持）
    scenes/                  场景 .tscn（选人背景/战斗/商店/失败/火堆）
    assets/images/           会被打进 pck 的成品美术
.artwork/raw/                美术源图（不打包）
pack/MyChar/                 暂存 + 部署用的三个文件
tools/                       构建与美术处理脚本（含 pck_append.py）
docs/                        说明文档（05 角色设计 / 08 卡牌清单为权威定稿）
ref/                         BaseLib / sts2 反编译源码（查 API 用）
```

## 产物

构建后得到三个平铺的文件，放进 `mods\MyChar\`：

- `MyChar.json` — manifest
- `MyChar.dll` — 程序集
- `MyChar.pck` — Godot 资源包（贴图由 PckPacker 打包，场景 `.tscn` 由 `tools/pck_append.py` 追加）

## 场景美术

选人背景、战斗立绘、商店、失败、火堆休息五类场景图已接入，替换了原版战士占位：

- 场景是文本格式 `.tscn`，直接追加进 mod pck（本体与皮肤 mod 验证过此加载方式）；
- 战斗立绘为静态图：`StaticBodyVisualsPatch` 在玩家死亡/复活时切换 defeat 立绘，无 Spine 骨骼也安全；
- 场景源图放 `.artwork/raw/`，跑 `tools/make_assets.py` 生成规格化成品图。


## 注意

`dotnet build` 直接可用，**不需要装 .NET 9 运行时，也不需要 Godot**。
场景 `.tscn` 为手写文本格式，直接追加进 pck，无需 Godot 导出。
