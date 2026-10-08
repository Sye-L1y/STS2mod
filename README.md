# MyChar · 杀戮尖塔 2 新角色 mod

新增可玩角色「阿尔法」，含独立卡池、起始卡组、遗物池、药水池，以及自带的占位美术。

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
    assets/images/           会被打进 pck 的成品美术
.artwork/raw/                美术源图（不打包）
pack/MyChar/                 暂存 + 部署用的三个文件
tools/                       构建与美术处理脚本
docs/                        说明文档
ref/                         BaseLib / sts2 反编译源码（查 API 用）
```

## 产物

构建后得到三个平铺的文件，放进 `mods\MyChar\`：

- `MyChar.json` — manifest
- `MyChar.dll` — 程序集
- `MyChar.pck` — Godot 资源包

## 文档

| 文档 | 内容 |
| --- | --- |
| [01-工具链与构建](docs/01-工具链与构建.md) | 环境、构建、PckPacker 的 .NET 版本坑、pck 路径映射 |
| [02-美术资源规格与接入](docs/02-美术资源规格与接入.md) | 尺寸规格、目录规范、代码接入点 |
| [03-本地化约定](docs/03-本地化约定.md) | loc key 推导、占位符、多语言 |
| [04-内容清单与下一步](docs/04-内容清单与下一步.md) | 已实现内容、占位项、后续优先级 |

## 注意

`dotnet build` 直接可用，**不需要装 .NET 9 运行时，也不需要 Godot**。
只有做战斗立绘等 `.tscn` 场景资源时才需要 Godot（见 01）。
