# STS2 Mod 模板

本目录提供两个开箱即用的 Slay the Spire 2 Mod 模板，**中文优先**，帮助社区快速上手。

## 模板列表

| 模板 | 说明 | 适用场景 |
|------|------|----------|
| `CharacterModTemplate/` | 人物 Mod 模板 | 新增可玩角色（含卡牌、遗物、能力、完整 Harmony 补丁） |
| `GeneralModTemplate/` | 通用 Mod 模板 | 功能类 Mod（修改器、工具、UI 增强等） |

## 快速开始

```bash
# 假设当前目录是 mods_src/mod_templates

# 1. 复制模板到 mods_src 下
cp -r CharacterModTemplate ../MyNewCharacter

# 2. 全局搜索替换名称（见各模板 README.md）

# 3. 修改 .csproj 和 build.ps1 中的路径（少一层 ..）

# 4. 编译
cd ../MyNewCharacter
powershell -ExecutionPolicy Bypass -File build.ps1
```

## 环境要求

- .NET 9.0 SDK
- Python 3.x（用于 PCK 打包）
- Pillow（`pip install Pillow`，可选，用于生成占位图）

## 跨平台支持

模板默认使用 `AnyCPU` 平台目标，编译出的 DLL 同时兼容 PC（x86_64）和移动端（ARM64 Android）。
STS2 在所有平台上都通过 `AssemblyLoadContext.LoadFromAssemblyPath()` 加载 Mod DLL，
因此同一个 DLL 无需修改即可在 PC 和手机上运行。

> **注意**：请勿将 `PlatformTarget` 改为 `x64`，否则 Mod 将无法在手机上加载。

## 从 STS1 迁移？

如果你之前开发过一代的角色 Mod（使用 BaseMod + ModTheSpire），请参阅各模板中的 **STS1 迁移指南** 章节。主要变化包括：

- **语言**: Java/Kotlin → C# 12
- **框架**: BaseMod 事件订阅 → Harmony 方法拦截
- **异步**: Action 队列 → async/await
- **本地化**: `!D!` 占位符 → `{Damage:diff()}` 格式
- **资产**: JAR 内嵌 → Godot PCK 打包

详细迁移对照表见 `CharacterModTemplate/README.md`。
