# 人物 Mod 模板

创建自定义可玩角色的完整模板，包含卡牌、遗物、能力、卡池和所有必要的 Harmony 补丁。

## 快速开始

1. 复制本文件夹到 `mods_src/你的角色名/`
2. 全局替换：
   - `MyCharacter` → `你的角色名`（类名、文件名）
   - `MyCharacterMod` → `你的命名空间`
   - `mycharacter` → `你的角色名小写`（资产路径）
   - `MY_CHARACTER` → `你的角色 ID（全大写下划线）`（本地化键）
   - `my_character` → `你的角色 ID（小写下划线）`（顶栏图标文件名）
   - `yourname.mycharacter` → `你的名字.你的mod`（Harmony ID）
3. 重命名 `MyCharacter.csproj` → `你的角色名.csproj`
4. 修改路径：
   - `.csproj` 中 HintPath：`..\..\..\` → `..\..\`（因为从 mod_templates 子目录移到了 mods_src 下）
   - `build.ps1` 中 `$repoRoot`：`"..\..\..\"` → `"..\..\"`
   - `build.ps1` 中 `$modName = "你的角色名"`
5. 更新 `mod_manifest.json`
6. 重命名本地化目录：`assets/MyCharacter/` → `assets/你的角色名/`
7. 编译：`powershell -ExecutionPolicy Bypass -File build.ps1`

## 项目结构

```
你的角色名/
  ├── 你的角色名.csproj           # .NET 9.0 项目文件
  ├── build.ps1                    # 构建脚本（仅 Windows）
  ├── mod_manifest.json            # Mod 元数据
  ├── generate_placeholders.py     # 占位图生成器
  │
  ├── MyCharacterBootstrap.cs      # 入口点（ModInitializer + Harmony）
  ├── MyCharacterModel.cs          # 角色定义（CharacterModel）
  ├── MyCharacterBaseModels.cs     # 基类（卡牌、遗物）
  ├── MyCharacterPools.cs          # 卡牌池 / 遗物池 / 药水池
  ├── MyCharacterCards.cs          # 卡牌实现
  ├── MyCharacterRelics.cs         # 遗物实现
  ├── MyCharacterPowers.cs         # 能力实现
  ├── MyCharacterPatches.cs        # Harmony 补丁（重要！）
  │
  └── assets/你的角色名/localization/
        ├── eng/                   # 英文本地化
        │   ├── cards.json
        │   ├── characters.json
        │   ├── relics.json
        │   └── powers.json
        └── zhs/                   # 简体中文
            └── (同上)
```

## 核心概念

### ModelDb 名称冲突（关键！）

`ModelDb.GetId(type)` **只使用类名**（忽略命名空间）来生成模型 ID。
如果你的 Mod 类名和基础游戏类名相同，会抛出 `DuplicateModelException`。

**解决方案**：给冲突的类名加 `_P` 后缀。

```csharp
// 错误：基础游戏已有 Strike 类
public class Strike : MyCard { }  // 会崩溃

// 正确：加后缀避免冲突
public class Strike_P : MyCard { }  // ID 变成 CARD.STRIKE_P
```

可在 `_tools/sts2_decomp/MegaCrit.Sts2.Core.Models.{Cards,Relics,Powers}/` 查看已有名称。

### 本地化键格式

键名格式为 `{模型ID}.字段`，模型 ID 由类名自动生成：
- `MyStrike` 类 → ID 条目 `MY_STRIKE` → 键 `MY_STRIKE.title`
- `SignatureStrike` → `SIGNATURE_STRIKE.title`

描述中的动态变量：
- `{Damage:diff()}` - 伤害值（含升级差异显示）
- `{Block:diff()}` - 格挡值
- `{MagicNumber:diff()}` - 魔法数字
- `[gold]文字[/gold]` - 金色关键字
- `[blue]{Amount}[/blue]` - 蓝色动态值

### 资产加载

STS2 使用 Godot PCK 格式打包 Mod 资产。但游戏的 ResourceLoader 无法加载 PCK 中的原始 PNG。
`MyCharacterPatches.cs` 中的补丁使用 `Image.LoadFromFile()` 绕过 ResourceLoader 直接加载纹理。

### 必需的 Harmony 补丁

模板包含人物 Mod **必须** 的所有补丁（都在 `MyCharacterPatches.cs` 中）：

| 补丁 | 作用 | 缺少会怎样 |
|------|------|------------|
| `AddCharacterPatch` | 将角色加入角色列表 | 角色不出现 |
| `ModAssetCachePatch` | 启用 Mod 纹理加载 | 所有图片加载失败 |
| `CharSelectIconPatch` | 角色选择按钮图标 | 选人界面无图标 |
| `CardPortraitPatch` | 卡牌立绘 | 卡牌无图 |
| `RelicIconPatch` | 遗物图标 | 遗物无图 |
| `PowerIconPatch` | 能力图标 | 能力无图 |
| `SfxPatch` (x3) | 音效（攻击/施法/死亡） | 无声或崩溃 |
| `ProgressSaveManager` (x3) | 跳过硬编码进度检查 | 存档崩溃 |
| `SerializationCachePatch` | 网络序列化注册 | 多人模式崩溃 |
| `ArchitectWinRunPatch` | 建筑师对话空值处理 | Boss 战后崩溃 |

### Spine 4 动画

STS2 使用 Spine 4 做角色动画。如果没有 Spine 数据：
1. 使用静态 Sprite2D 作为战斗角色视觉
2. `GenerateAnimator` 方法映射 Spine 状态到游戏动作
3. 参考 Watcher Mod 了解如何移植 STS1 的 Spine 数据

---

## STS1 → STS2 迁移指南

> 如果你之前用 BaseMod + ModTheSpire 开发过一代人物 Mod（如 Marisa、Hermit 等），
> 以下对照表帮你快速理解二代的变化。

### 总体架构对比

| 方面 | STS1 | STS2 |
|------|------|------|
| **语言** | Java / Kotlin (JVM 8) | C# 12 (.NET 9.0) |
| **引擎** | libGDX | Godot 4.5 |
| **构建** | Gradle / Maven → JAR | dotnet build → DLL + PCK |
| **Mod 框架** | BaseMod + ModTheSpire | 内置 Mod 支持 + Harmony |
| **Hook 机制** | 事件订阅 (`Subscriber` 接口) | 方法拦截 (`[HarmonyPatch]`) |
| **异步模型** | Action 队列 (`addToBot/addToTop`) | `async/await` |
| **资产打包** | JAR 内嵌资源 | Godot PCK 文件 |
| **注册方式** | `BaseMod.addCard()` 显式注册 | `ModelDb` 反射自动发现 |

### Mod 入口对比

**STS1：**
```kotlin
@SpireInitializer
class MyMod : PostInitializeSubscriber, EditCardsSubscriber, ... {
    companion object {
        @JvmStatic fun initialize() { MyMod() }
    }
    init {
        BaseMod.subscribe(this)
        BaseMod.addColor(MARISA_COLOR, ...)
    }
    override fun receiveEditCards() {
        BaseMod.addCard(Strike_MRS())
        BaseMod.addCard(Defend_MRS())
    }
}
```

**STS2：**
```csharp
[ModInitializer(nameof(Init))]
public static class MyCharacterBootstrap
{
    private static bool _initialized;
    public static void Init()
    {
        if (_initialized) return;
        _initialized = true;
        new Harmony("yourname.mycharacter").PatchAll(Assembly.GetExecutingAssembly());
    }
}
// 卡牌在 CardPoolModel.GenerateAllCards() 中注册，无需逐个 addCard
```

### 角色定义对比

**STS1：**
```kotlin
class Marisa(name: String) : CustomPlayer(name, ThModClassEnum.MARISA, ...) {
    override fun getStartingDeck() = arrayListOf("Strike_MRS", "Defend_MRS", ...)
    override fun getStartingRelics() = arrayListOf("MiniHakkero")
    override fun getLoadout() = CharSelectInfo("Marisa", "描述", 75, 75, 99, 5, ...)
}
```

**STS2：**
```csharp
public sealed class MyCharacter : CharacterModel
{
    public override int StartingHp => 75;
    public override int StartingGold => 99;
    public override IEnumerable<CardModel> StartingDeck => [
        ModelDb.Card<MyStrike>(), ModelDb.Card<MyDefend>(), ...
    ];
    public override IReadOnlyList<RelicModel> StartingRelics => [ModelDb.Relic<StarterRelic>()];
}
```

**迁移要点：**
- 不再用字符串 ID 引用，改用泛型 `ModelDb.Card<T>()`
- 角色属性从方法改为属性（`override get`）
- 不再需要 `@SpireEnum` 扩展枚举
- 颜色直接用 `Godot.Color`，不再需要 `BaseMod.addColor()`

### 卡牌实现对比

**STS1（打击）：**
```kotlin
class Strike_MRS : CustomCard(ID, NAME, IMG, 1, DESC, ATTACK, MARISA_COLOR, BASIC, ENEMY) {
    init {
        baseDamage = 6
        tags.add(CardTags.STARTER_STRIKE)
    }
    override fun use(p: AbstractPlayer, m: AbstractMonster?) {
        addToBot(DamageAction(m, DamageInfo(p, damage, damageTypeForTurn), SLASH_DIAGONAL))
    }
    override fun upgrade() {
        if (!upgraded) { upgradeName(); upgradeDamage(3) }
    }
}
```

**STS2（打击）：**
```csharp
public sealed class MyStrike : MyCharacterCard
{
    protected override HashSet<CardTag> CanonicalTags => [CardTag.Strike];
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(6m, ValueProp.Move)];

    public MyStrike() : base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy) { }

    protected override async Task OnPlay(PlayerChoiceContext ctx, CardPlay play)
    {
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this)
            .Targeting(play.Target!).WithHitFx("vfx/vfx_attack_slash").Execute(ctx);
    }

    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3m);
}
```

**迁移要点：**
- 伤害/格挡值通过 `DynamicVar` 声明，不再是 `baseDamage` 字段
- `use()` → `OnPlay()`，变成 `async Task`
- `addToBot(DamageAction(...))` → `await DamageCmd.Attack(...).Execute(ctx)`
- `upgrade()` → `OnUpgrade()`，`upgradeDamage(3)` → `DynamicVars.Damage.UpgradeValueBy(3m)`
- 不需要手动 `upgradeName()`，框架自动处理

### 常用 Action 迁移速查

| STS1 Action | STS2 等效写法 |
|------------|--------------|
| `DamageAction(m, info, fx)` | `DamageCmd.Attack(dmg).FromCard(this).Targeting(target).WithHitFx(fx).Execute(ctx)` |
| `DamageAllEnemiesAction(...)` | `DamageCmd.Attack(dmg).FromCard(this).TargetingAllEnemies().Execute(ctx)` |
| `GainBlockAction(p, amount)` | `CreatureCmd.GainBlock(creature, blockVar, cardPlay)` |
| `ApplyPowerAction(p, p, power, amt)` | `PowerCmd.Apply<PowerType>(target, amount, applier, cardSource)` |
| `DrawCardAction(count)` | `CardPileCmd.Draw(ctx, count, owner)` |
| `GainEnergyAction(amount)` | `PlayerCmd.GainEnergy(owner, amount)` |
| `MakeTempCardInHandAction(card)` | `CardPileCmd.AddGeneratedCardToCombat(card, PileType.Hand, true)` |
| `ExhaustSpecificCardAction(card)` | `CardPileCmd.Exhaust(card)` |
| `DiscardAction(p, amount)` | `CardPileCmd.DiscardFromHand(ctx, owner, amount)` |
| `RemoveSpecificPowerAction(p, id)` | `PowerCmd.Remove<PowerType>(creature)` |

### 能力 (Power) 对比

**STS1：**
```kotlin
class ChargeUpPower(owner: AbstractCreature, val cnt: Int) : AbstractPower() {
    init {
        this.owner = owner; amount = cnt; type = PowerType.BUFF
        img = Texture("img/powers/chargeup.png")
    }
    override fun atDamageFinalGive(damage: Float, type: DamageType): Float {
        return if (cnt > 0 && type == NORMAL) damage * 2f else damage
    }
    override fun stackPower(stackAmount: Int) { amount += stackAmount; updateDescription() }
}
```

**STS2：**
```csharp
public sealed class ChargeUpPower : PowerModel
{
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.Counter;

    public override decimal ModifyDamageMultiplicative(
        Creature? target, decimal amount, ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (dealer == Owner && props.HasFlag(ValueProp.Move))
            return 2m;
        return 1m;
    }
}
```

**迁移要点：**
- 堆叠类型：`amount += x` → `PowerStackType.Counter`（框架自动处理）
- 伤害修改：`atDamageFinalGive()` → `ModifyDamageMultiplicative()` 或 `ModifyDamageAdditive()`
- 格挡修改：`modifyBlock()` → `ModifyBlockAdditive()` 或 `ModifyBlockMultiplicative()`
- 图标不再手动加载，通过 Harmony 补丁拦截 `PowerModel.get_Icon`

### 遗物 (Relic) 对比

**STS1：**
```kotlin
class MiniHakkero : CustomRelic(ID, loadImage(IMG), loadImage(OUTLINE), STARTER, MAGICAL) {
    override fun onUseCard(card: AbstractCard, action: UseCardAction) {
        flash()
        addToTop(ApplyPowerAction(p, p, ChargeUpPower(p, 1), 1))
    }
}
```

**STS2：**
```csharp
public sealed class StarterRelic : MyCharacterRelic
{
    public StarterRelic() : base("starter_relic") { }
    public override RelicRarity Rarity => RelicRarity.Starter;

    public override async Task BeforeCombatStart()
    {
        Flash();
        await CreatureCmd.GainBlock(Owner.Creature, 3, 0, null);
    }
}
```

**遗物生命周期钩子迁移：**

| STS1 | STS2 |
|------|------|
| `atBattleStart()` | `BeforeCombatStart()` |
| `atTurnStart()` | `AfterPlayerTurnStart(ctx, player)` |
| `atTurnStartPostDraw()` | （同上，在 `AfterPlayerTurnStart` 中处理） |
| `onUseCard(card, action)` | `AfterCardPlayed(cardPlay, model)` |
| `onPlayerEndTurn()` | `AfterPlayerTurnEnd()` |
| `onMonsterDeath(m)` | `AfterCreatureDied(creature)` |
| `flash()` | `Flash()` |
| `counter` | `DynamicVars["Counter"]`（或用自定义字段） |

### 本地化格式对比

**STS1（`cards.json`）：**
```json
{
  "Strike_MRS": {
    "NAME": "Simple Spark",
    "DESCRIPTION": "Deal !D! damage."
  }
}
```

**STS2（`cards.json`）：**
```json
{
  "MY_STRIKE.title": "打击",
  "MY_STRIKE.description": "造成 {Damage:diff()} 点伤害。"
}
```

**占位符迁移：**

| STS1 | STS2 | 说明 |
|------|------|------|
| `!D!` | `{Damage:diff()}` | 伤害值 |
| `!B!` | `{Block:diff()}` | 格挡值 |
| `!M!` | `{MagicNumber:diff()}` 或自定义 | 魔法数字 |
| `[E]` | `[W]` | 能量图标 |
| `NL` | `\n` | 换行 |
| `#y文字 ` | `[gold]文字[/gold]` | 金色关键字 |
| `#b文字 ` | `[blue]文字[/blue]` | 蓝色数值 |
| `#r文字 ` | `[red]文字[/red]` | 红色文字 |

### 自定义枚举 → 无需枚举

**STS1** 需要 `@SpireEnum` 扩展游戏枚举：
```kotlin
object AbstractCardEnum {
    @SpireEnum lateinit var MARISA_COLOR: CardColor
}
object ThModClassEnum {
    @SpireEnum var MARISA: PlayerClass? = null
}
```

**STS2** 不需要枚举扩展。`ModelDb` 通过反射自动发现你的 `CharacterModel`、`CardModel` 等子类。
只需在 `CardPoolModel` 中注册卡牌，在 `Harmony` 补丁中将角色加入列表即可。

### 资产打包对比

| 方面 | STS1 | STS2 |
|------|------|------|
| **格式** | JAR 内嵌 (resources/) | Godot PCK 文件 |
| **图片加载** | `ImageMaster.loadImage(path)` | `Image.LoadFromFile(path)` + Harmony 拦截 |
| **动画** | Spine (libGDX) | Spine 4 (Godot) |
| **音效** | OGG 文件 + libGDX Audio | FMOD 事件系统 |
| **卡面** | 250x384 PNG | 各种尺寸 PNG（需要提供立绘路径） |
| **卡背** | 需提供攻击/技能/能力三套 | 通过 `CardFrameMaterialPath` 复用游戏内卡背 |

### 构建系统对比

**STS1（Gradle）：**
```bash
gradle jar          # 编译打包 JAR
# 手动复制到 Steam/workshop/ 目录
```

**STS2（dotnet + Python）：**
```bash
powershell -ExecutionPolicy Bypass -File build.ps1
# 自动：dotnet build → 复制 DLL → 打包 PCK → 输出到 mods/ 目录
```

### 迁移检查清单

- [ ] 角色类：`CustomPlayer` → `CharacterModel`
- [ ] 卡牌类：`CustomCard` → `CardModel` 子类
- [ ] 遗物类：`CustomRelic` → `RelicModel` 子类
- [ ] 能力类：`AbstractPower` → `PowerModel`
- [ ] 动作：`addToBot(Action)` → `await XxxCmd.Xxx()`
- [ ] 检查类名冲突，必要时加 `_P` 后缀
- [ ] 本地化占位符转换：`!D!` → `{Damage:diff()}`
- [ ] 资产路径从 JAR 内路径改为 `res://` 路径
- [ ] 卡牌注册从 `BaseMod.addCard()` 改为 `CardPoolModel.GenerateAllCards()`
- [ ] 添加所有必需的 Harmony 补丁（见 MyCharacterPatches.cs）
- [ ] 图片从 JAR 内嵌改为 PCK 打包
- [ ] Spine 动画数据转换（如有）

## API 速查表

| 操作 | 代码 |
|------|------|
| 单体攻击 | `DamageCmd.Attack(dmg).FromCard(this).Targeting(target).Execute(ctx)` |
| AOE 攻击 | `.TargetingAllEnemies()` |
| 获得格挡 | `CreatureCmd.GainBlock(creature, BlockVar, cardPlay)` |
| 施加能力 | `PowerCmd.Apply<StrengthPower>(target, amount, applier, cardSource)` |
| 抽牌 | `CardPileCmd.Draw(ctx, count, owner)` |
| 获得能量 | `PlayerCmd.GainEnergy(owner, amount)` |
| 生成卡牌 | `combatState.CreateCard<T>(owner)` + `CardPileCmd.AddGeneratedCardToCombat(...)` |
| X 费卡 | `HasEnergyCostX => true`，构造函数费用 `-1`，`ResolveEnergyXValue(ctx)` |
| 消耗 | `CanonicalKeywords => [CardKeyword.Exhaust]` |
| 保留 | `CanonicalKeywords => [CardKeyword.Retain]` |

## 音效复用

可以复用游戏内已有的音效事件路径：
- 角色：`event:/sfx/characters/{name}/{name}_{action}`
  - 动作：`select`, `attack`, `cast`, `die`
  - 名称：`ironclad`, `silent`, `defect`, `necrobinder`, `architect`
- VFX：`vfx/vfx_attack_slash`, `vfx/vfx_attack_blunt`, `vfx/vfx_bloody_impact`

## 编译要求

- .NET 9.0 SDK
- Python 3.x（用于 PCK 打包）
- Pillow（`pip install Pillow`）- 可选，用于生成占位图
- 游戏 DLL 位于 `data_sts2_windows_x86_64/`（.csproj 自动引用）

## 跨平台兼容

项目使用 `AnyCPU` 平台目标，编译出的 DLL 同时兼容 PC（x86_64）和移动端（ARM64 Android）。
无需分别编译，同一个 DLL 即可在所有平台上运行。

> **注意**：请勿将 `.csproj` 中的 `PlatformTarget` 改为 `x64`，否则 Mod 将无法在手机上加载。
