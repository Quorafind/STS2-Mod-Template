# 通用 Mod 模板

适用于功能类 Mod（修改器、工具、UI 增强等）的最小化模板。

## 快速开始

1. 复制本文件夹到 `mods_src/你的Mod名/`
2. 全局替换：
   - `MyMod` → `你的Mod名`（类名、文件名、manifest）
   - `MyModNamespace` → `你的命名空间`
   - `yourname.mymod` → `你的名字.你的mod`（Harmony ID）
3. 重命名 `MyMod.csproj` → `你的Mod名.csproj`
4. 修改路径（从 mod_templates 移到 mods_src 后）：
   - `.csproj` 中 HintPath：`..\..\..\` → `..\..\`
   - `build.ps1` 中 `$repoRoot`：`"..\..\..\"` → `"..\..\"`
   - `build.ps1` 中 `$modName = "你的Mod名"`
5. 更新 `mod_manifest.json`
6. 编译：`powershell -ExecutionPolicy Bypass -File build.ps1`

## 项目结构

```
你的Mod/
  ├── 你的Mod.csproj              # .NET 9.0 项目文件
  ├── build.ps1                    # 构建脚本（仅 Windows）
  ├── mod_manifest.json            # Mod 元数据
  ├── MyModBootstrap.cs            # 入口点
  ├── MyModPatches.cs              # Harmony 补丁（你的逻辑）
  └── assets/你的Mod/              # 本地化 + 图片
      └── localization/{eng,zhs}/
```

## Mod 工作原理

### 入口点
游戏启动时扫描所有已加载 DLL，找到带 `[ModInitializer(方法名)]` 特性的类，
调用指定的静态方法。在这里：
1. 通过 `Harmony.PatchAll()` 应用所有补丁
2. 初始化 Mod 状态

### Harmony 补丁

Harmony 是修改游戏行为的核心机制。三种补丁类型：

| 类型 | 时机 | 用途 |
|------|------|------|
| `Prefix` | 原方法执行**前** | 修改参数、跳过原方法（`return false`） |
| `Postfix` | 原方法执行**后** | 修改返回值、追加行为 |
| `Transpiler` | 重写 IL 字节码 | 高级用法：修改方法体 |

```csharp
[HarmonyPatch(typeof(游戏类), nameof(游戏类.方法))]
internal static class 我的补丁
{
    // Prefix：在原方法前执行。返回 false 可跳过原方法。
    static bool Prefix(游戏类 __instance, ref int 参数) { ... }

    // Postfix：在原方法后执行。用 ref __result 修改返回值。
    static void Postfix(ref 返回类型 __result) { ... }
}
```

### 访问私有成员

```csharp
// 字段引用（缓存，高性能）：
static readonly AccessTools.FieldRef<类型, 字段类型> Ref =
    AccessTools.FieldRefAccess<类型, 字段类型>("_私有字段名");
// 读取: 字段类型 val = Ref(instance);
// 写入: Ref(instance) = newValue;
```

### 游戏架构概览

| 命名空间 | 内容 |
|----------|------|
| `MegaCrit.Sts2.Core.Models` | 数据模型（卡牌、遗物、能力、角色） |
| `MegaCrit.Sts2.Core.Models.Cards` | 基础游戏卡牌定义 |
| `MegaCrit.Sts2.Core.Models.Powers` | 能力定义（力量、敏捷等） |
| `MegaCrit.Sts2.Core.Entities` | 运行时实体（玩家、生物、卡牌实例） |
| `MegaCrit.Sts2.Core.Commands` | 游戏指令（DamageCmd、CreatureCmd、CardPileCmd） |
| `MegaCrit.Sts2.Core.Nodes` | Godot 场景节点（NGame、NRun、NCard 等） |
| `MegaCrit.Sts2.Core.Runs` | 运行状态管理 |
| `MegaCrit.Sts2.Core.Context` | LocalContext - 获取当前玩家/运行 |

### 常用补丁切入点

| 类.方法 | 调用时机 | 常见用途 |
|---------|---------|---------|
| `NGame._Input` | 每个输入事件 | 添加快捷键 |
| `NRun._Process` | 运行期间每帧 | 持续效果 |
| `CharacterModel.get_StartingHp` | 角色初始化 | 修改属性 |
| `CardModel.OnPlay` | 打出卡牌 | 修改卡牌效果 |
| `PowerModel.AfterApplied` | 获得能力 | 响应增益/减益 |

### PCK 文件
每个 Mod 都需要 `.pck` 文件（Godot 资源包），至少包含：
- `mod_manifest.json` - Mod 元数据
- 本地化文件（如有）
- 图片/音频资产（如有）

`_tools/pack_godot_pck.py` 负责 PCK 创建。

## 编译要求
- .NET 9.0 SDK
- Python 3.x（用于 PCK 打包）
- 游戏 DLL 位于 `data_sts2_windows_x86_64/`（自动引用）