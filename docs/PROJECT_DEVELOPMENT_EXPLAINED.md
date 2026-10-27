# 霓虹竞技场完整开发解释

> **GitHub 展示版**：本文件与作者本地学习文档保持同步，用于公开说明项目架构、调用链、v1.4 AI 设计与验证边界。项目入口和运行方式见 [README](../README.md)。

这是一份持续更新的学习文档，用来解释《霓虹竞技场》从启动到通关的完整代码流程。它不要求先记住所有脚本，而是帮助你建立“玩家操作发生后，哪些对象、组件和数据依次工作”的整体认识。

当前解释对应 `v1.4.0` 发布候选：在 `v1.3.0` 完整流程上加入 Boss Utility AI、有边界的动态战斗导演和开发决策面板。后续新增功能时继续更新本文件，不再另起一套互相割裂的说明。

## 一、先用一句话理解游戏

《霓虹竞技场》是一款 2D 生存射击游戏：

```text
开始游戏
→ 选择第一把武器
→ 玩家移动，武器自动索敌
→ 击败敌人并拾取经验
→ 三选一升级
→ 集齐六把武器
→ 小首领战
→ 金色六选二强化
→ 最终首领战
→ 胜利或死亡结算
```

项目重点不是复杂美术，而是展示：

- Unity 生命周期和组件协作；
- Input System 与物理移动；
- 数据驱动的武器、敌人和升级；
- 自动索敌和多武器独立射击；
- 对象池；
- 委托与事件；
- 游戏阶段控制；
- UI、设置和本地化；
- 自动测试和多平台构建。

## 二、项目的总体结构

核心代码可以分为九组。

| 系统 | 主要脚本 | 负责什么 |
|---|---|---|
| 游戏流程 | `GameManager`、`BossEncounter` | 开始、暂停、死亡、胜利和首领阶段 |
| 玩家 | `PlayerMovement`、`Health`、`PlayerProgress` | 移动、生命、经验和等级 |
| 武器 | `PlayerShooter`、`WeaponDefinition`、`Projectile` | 装备、索敌、冷却、发射和伤害 |
| 敌人 | `EnemySpawner`、`EnemyController`、`EnemyDefinition` | 刷怪、行为、受伤、死亡和经验掉落 |
| Boss AI | `BossAgent`、`BossUtilityBrain`、`BossBehavior` 及六个行为类 | 收集战场情境、评分、选择并执行首领行为 |
| 战斗导演 | `CombatDirector`、`CombatDirectorModel`、`CombatDirectorConfig` | 计算有限压力，调整刷怪节奏、批次、上限和权重 |
| 升级 | `UpgradeController`、`PlayerUpgradeApplier`、`PlayerUpgradeData` | 生成升级选项并应用效果 |
| 性能 | `GameObjectPool`、`PoolMember`、`EnemyRegistry` | 复用高频对象并维护敌人集合 |
| 表现与平台 | `HudController`、`CombatFeedback`、`CombatDebugOverlay`、`PortfolioSettings`、移动端脚本 | UI、反馈、调试、设置、本地化和触屏输入 |

可以把整体关系理解为：

```text
GameManager 管理整局生命周期
├─ PlayerMovement 读取输入并移动
├─ PlayerShooter 根据武器配置自动攻击
├─ CombatDirector 读取战斗状态并给出受限制的刷怪参数
├─ EnemySpawner 使用导演参数生成敌人
├─ PlayerProgress 接收经验并请求升级
├─ UpgradeController 暂停游戏并显示选项
├─ BossEncounter 在六武器后接管战斗阶段
└─ BossAgent 通过 BossUtilityBrain 选择并执行首领行为
```

## 三、游戏启动时发生了什么

### 3.1 场景入口

项目只有一个主要游戏场景：

```text
Assets/Scenes/Main.unity
```

Unity 加载场景后，会调用场景内所有启用组件的生命周期方法。`GameManager` 是整局流程的入口。

### 3.2 GameManager.Awake

`Awake()` 在对象初始化时执行。这里完成的是“游戏暂时不能开始”的初始状态：

- `Time.timeScale = 0`，暂停游戏时间；
- 关闭移动、射击和刷怪组件；
- 显示开始界面；
- 隐藏暂停和结算界面；
- 给按钮注册点击事件；
- 创建并初始化 `BossEncounter`。

为什么不一进入场景就启用全部系统？

因为玩家还没有点击开始，也没有选择第一把武器。如果刷怪和计时提前运行，玩家会在菜单阶段被攻击。

### 3.3 GameManager.Start

`Start()` 在场景中所有 `Awake()` 基本完成后执行，适合连接已经初始化好的对象：

- `HudController.Initialize()` 绑定生命值和经验数据；
- 订阅玩家死亡事件；
- 订阅敌人全局死亡事件；
- 订阅玩家等级变化事件；
- 从 `PlayerPrefs` 读取历史最佳时间和击杀数。

这里体现了事件通信：`GameManager` 不需要每帧检查“玩家是不是死了”，而是由 `Health` 在死亡时主动通知。

## 四、从点击开始到正式战斗

点击开始按钮后调用：

```text
GameManager.StartRun
```

执行顺序：

1. 隐藏开始界面；
2. 将状态标记为正在选择初始武器；
3. 调用 `UpgradeController.ShowStartingWeaponChoices()`；
4. 显示三种武器选项；
5. 玩家选择后装备第一把武器；
6. 回调 `GameManager.BeginRun()`；
7. 恢复 `Time.timeScale = 1`；
8. 启用移动、射击、刷怪和移动端操作。

调用链可以写成：

```text
开始按钮
→ GameManager.StartRun
→ UpgradeController.ShowStartingWeaponChoices
→ 玩家点击武器按钮
→ PlayerShooter.EquipWeapon
→ GameManager.BeginRun
```

这也是回调的实际用途：`GameManager` 发起选择，但不知道玩家什么时候选完；`UpgradeController` 完成选择后再通知它继续。

## 五、玩家为什么能够移动

`PlayerMovement` 依赖：

- `Rigidbody2D`；
- `InputActionAsset`；
- `ArenaBounds`。

### 5.1 Awake 获取依赖

`Awake()` 中使用 `GetComponent<Rigidbody2D>()` 缓存刚体，并从 Input Action Asset 查找 `Player/Move`。

组件只获取一次，而不是放在 `Update()` 中每帧查询。

### 5.2 Update 读取输入

```csharp
input = moveAction.ReadValue<Vector2>();
input = Vector2.ClampMagnitude(input, 1f);
```

`ClampMagnitude` 避免同时按两个方向时斜向速度变快。

### 5.3 FixedUpdate 执行物理移动

```text
当前位置
+ 输入方向 × 移动速度 × 固定时间步长
→ ArenaBounds 限制范围
→ Rigidbody2D.MovePosition
```

输入放在 `Update()` 读取，刚体移动放在 `FixedUpdate()` 执行，这是因为键盘输入按帧更新，而物理系统按固定时间步运行。

### 5.4 桌面和手机为什么能共用逻辑

Android 浮动摇杆最终把触摸转换为虚拟 Gamepad 的左摇杆输入。`PlayerMovement` 仍然只读取 `Player/Move`，不需要分别写电脑移动和手机移动。

## 六、武器系统如何工作

### 6.1 WeaponDefinition 保存静态配置

每种武器的以下数据保存在 ScriptableObject 中：

- 武器类型；
- 图标和颜色；
- 单发伤害；
- 射击间隔；
- 射程；
- 弹速。

ScriptableObject 是项目资源，不挂载到 GameObject。它适合保存“所有同类武器共享的静态定义”。

### 6.2 PlayerShooter 保存运行时状态

`PlayerShooter` 内部的 `EquippedWeapon` 保存：

- 引用哪一种 `WeaponDefinition`；
- 使用哪一个武器槽位；
- 下一次允许开火的时间。

同一类型武器可以装备多把，因为每一个 `EquippedWeapon` 都有独立冷却和独立槽位。

### 6.3 六个槽位

玩家周围预先设置六个槽位。装备武器时：

1. 取当前装备数量作为槽位下标；
2. 设置槽位图标和颜色；
3. 显示槽位；
4. 把新武器加入 `equippedWeapons` 列表。

总上限由槽位数组长度决定，不限制每种武器最多两把，因此玩家可以形成偏科构筑。

### 6.4 每帧如何自动射击

`PlayerShooter.Update()` 遍历所有已装备武器：

```text
检查独立冷却
→ 计算升级后的有效射程
→ TargetSelector 查找目标
→ 旋转武器槽位
→ 从对象池获取子弹
→ Projectile.Fire
→ 记录下一次开火时间
```

如果当前没有目标，不会每帧立刻重复扫描，而是使用较短的空闲扫描间隔。

## 七、自动索敌为什么不是简单找最近敌人

`EnemyRegistry` 保存当前处于活动状态的敌人。敌人生成时注册，回收时移除。

这样 `TargetSelector` 可以直接遍历有效敌人，不必调用昂贵的全场景搜索。

当前目标选择逻辑分两步：

1. 找到射程内最近一圈的距离；
2. 在这圈附近的优先带中选择绝对生命值最低的敌人。

这样既不会攻击很远但残血的敌人，也不会完全忽略附近更容易击杀的目标。

每把武器使用自己的有效射程，因此不同槽位可以同时攻击不同敌人。

## 八、子弹如何造成伤害并回收

子弹通过 `GameObjectPool.Get()` 获取，`Projectile.Fire()` 接收：

- 飞行方向；
- 武器定义；
- 当前伤害倍率；
- 当前射程倍率。

子弹记录出生位置。它会在以下情况回收：

- 命中有效敌人；
- 飞行距离达到武器有效射程；
- 触发其他生命周期结束条件。

这解决了“子弹飞出画面后继续存在”和“打死屏幕外敌人却无法拾取经验”的早期问题。

## 九、敌人是怎样生成和行动的

### 9.1 EnemyDefinition

每类敌人的静态配置包含：

- 最大生命；
- 移动速度；
- 接触伤害；
- 经验值；
- 视觉颜色和经验物形状；
- 行为类型。

目前有 Chaser、Dasher 和 Berserker 三类。

### 9.2 EnemySpawner 与 CombatDirector

`EnemySpawner` 仍保留按时间缩短间隔的基线计算，但 v1.4 起优先使用 `CombatDirector` 给出的夹紧结果：

```text
CombatDirector 读取生命、近期伤害、击杀率、武器数、敌人数
→ CombatDirectorModel 平滑压力并产出间隔/批次/上限/权重倍数
→ EnemySpawner 检查恢复窗口与剩余容量
→ 从预警对象池取出标记
→ 等待约 1.5 秒后生成敌人
```

敌人类型仍按权重和解锁时间选择，但权重会乘以导演给出的组合倍数：玩家表现稳定时，Dasher 和 Berserker 的权重上限更高；恢复窗口内降低高级敌人权重。所有结果都有上下限，不会无限加快，也不会暗中修改玩家伤害。

出生位置在整个竞技场内随机选择，但必须与玩家保持最小距离。找不到合适位置时，选择距离玩家最远的角落。

### 9.3 EnemyController

敌人组件负责：

- 根据行为类型移动；
- 处理追击或冲刺；
- 接触玩家后造成固定伤害；
- 接收子弹伤害；
- 死亡时生成对应经验物；
- 通知全局击杀事件；
- 从注册表移除并回收到对象池。

普通敌人接触玩家后立即死亡，避免持续贴住玩家反复扣血。

## 十、经验和升级如何连接

经验物被玩家吸收后调用：

```text
PlayerProgress.AddExperience
```

`PlayerProgress` 更新经验值；达到阈值后：

1. 扣除当前等级需要的经验；
2. 等级加一；
3. 提高下一级需求；
4.触发 `LevelChanged`；
5. 触发 `LevelUpRequested`。

`UpgradeController` 订阅升级请求并执行：

```text
暂停时间
→ 从可用升级中过滤已经到上限的项目
→ 按等级规则生成三项选择
→ 显示升级界面
→ 玩家选择
→ PlayerUpgradeApplier 应用效果
→ 恢复游戏时间
```

升级配置使用 `PlayerUpgradeData`，具体效果由 `PlayerUpgradeApplier` 根据 `UpgradeEffectType` 分发给移动、生命、射击和拾取范围组件。

这种拆分让 UI 不直接修改玩家内部字段。

## 十一、首领闭环如何触发

`BossEncounter` 维护以下阶段：

```text
Collecting
MiniIntro
MiniFight
Rewards
FinalIntro
FinalFight
Victory
Defeat
```

当 `PlayerShooter.EquippedCount == 6`：

1. 停止普通刷怪；
2. 清理普通敌人和出生预警；
3. 显示首领到达预警；
4. 生成小首领；
5. 小首领死亡后直接增加两级；
6. 弹出六项金色强化，选择两项；
7. 生成最终首领；
8. 最终首领死亡后调用 `GameManager.WinRun()`。

### v1.4 Boss 决策方式

`BossAgent` 不再使用 `attackIndex % 3`。每次行动结束后，它收集一份 `BossSituation`：

- 双方位置与玩家刚体速度；
- 玩家和 Boss 距离场地边缘的程度；
- Boss 当前生命比例；
- 当前时间、是否为最终首领。

`BossUtilityBrain` 对六个候选行为分别评分：

| 行为 | 主要倾向 |
|---|---|
| `Approach` | 玩家较远时快速逼近 |
| `PredictiveDash` | 中距离、玩家有速度时预测落点并冲刺 |
| `FanVolley` | 中距离、玩家较稳定时用扇形弹幕封路 |
| `ZoneBombardment` | 玩家靠边或移动受限时提高区域轰炸分数 |
| `RingBarrage` | 仅在 Boss 半血后解锁，近中距离环形弹幕 |
| `Reposition` | 玩家贴脸且 Boss 靠边时重新拉开位置 |

分数先乘行为基础权重，再应用距离偏好、玩家速度/靠边修正和重复惩罚。冷却中的行为直接标记为不可用，不参与选择；平局按固定顺序处理，因此同一局面可复现，不是每帧无边界随机。

决策层只输出“做什么”。真正的移动、预警线、范围危险、弹丸和对象池获取仍由 `BossAgent` 与 `BossHazard` 执行。这样可以把“战术选择”和“物理执行”分开测试，也能确保 Boss 死亡时统一取消当前行为并释放危险对象。

## 十二、对象池为什么重要

高频对象包括：

- 子弹；
- 敌人；
- 经验物；
- 出生预警。

如果每次都 `Instantiate` 和 `Destroy`，会产生频繁的对象创建、销毁和垃圾回收压力。

对象池流程：

```text
启动时预热对象
→ Get 取出空闲对象
→ 重置位置和状态
→ 使用
→ PoolMember.Release 回收
→ 下次再次复用
```

对象池最容易出现的错误不是“取不出来”，而是旧状态没有重置，例如：

- 子弹保留上一次飞行距离；
- 敌人保留上一次生命值；
- 经验物仍引用旧目标；
- 一个对象被重复回收。

`PoolMember` 用来记录所属池并防止重复回收。

## 十三、UI、暂停和事件

UI 不应该自己决定游戏规则。主要关系是：

- 按钮通知 `GameManager`；
- `GameManager` 修改游戏阶段和时间；
- `Health`、`PlayerProgress` 通过事件通知 HUD；
- `UpgradeController` 控制升级界面和暂停；
- `BossEncounter` 控制首领血条和金色奖励。

暂停使用 `Time.timeScale = 0`，但界面输入仍然可以工作。计时使用 `Time.unscaledDeltaTime`，并由 `GameManager` 判断是否真的处于战斗状态。

## 十四、设置和本地化

`PortfolioSettings` 使用 `PlayerPrefs` 保存：

- 中文或英文；
- 声音开关；
- 总音量；
- 音乐音量；
- 音效音量。

设置变化时触发 `Changed` 事件，相关 UI 和音频系统进行刷新。

`LocalizedLabel` 管理中英文文本，不需要每个菜单脚本分别判断语言。

## 十五、测试和发布

当前 PlayMode 测试为 `18/18`。原有六项继续覆盖：

- 六武器触发小首领；
- 小首领奖励两级；
- 金色六选二；
- 最终首领和胜利；
- 受伤预警可躲避；
- 设置持久化。

v1.4 新增十二项，覆盖：

- Boss 每个行为都能独立启动并完成；
- Boss 死亡后停止决策并释放所有危险对象；
- 冷却中的技能不会被选择；
- 重复行为被扣分且不再连续选择；
- 玩家远离时更倾向追击；
- 玩家靠边时区域轰炸分数显著提高；
- 半血前环形弹幕不可用、半血后可用；
- 导演压力和输出不越界；
- 残血且刚受重伤时进入恢复窗口；
- 恢复不能在冷却内反复触发；
- 敌人达到上限后批次为 0；
- 实际场景中的导演会在玩家重伤后进入恢复。

发布流程还检查场景、对象池、ScriptableObject、版本号和平台设置，并生成 Windows、WebGL 和 Android 构建。

自动测试不能证明数值一定好玩，它主要证明关键规则没有在迭代后失效。数值和平衡仍需要真实试玩。

## 十六、完整调用链示例

### 示例一 玩家击败敌人并升级

```text
PlayerShooter.Update
→ TargetSelector.FindPriorityTarget
→ GameObjectPool.Get 子弹
→ Projectile.Fire
→ Projectile 命中 EnemyController
→ Health 扣血并死亡
→ EnemyController 生成 ExperiencePickup
→ 玩家吸收经验物
→ PlayerProgress.AddExperience
→ LevelUpRequested 事件
→ UpgradeController 打开三选一
→ PlayerUpgradeApplier.Apply
→ 恢复游戏
```

### 示例二 玩家死亡

```text
敌人接触玩家
→ Health.TakeDamage
→ Health.Died 事件
→ GameManager.HandlePlayerDied
→ 关闭移动、射击、刷怪
→ Time.timeScale = 0
→ 保存最高纪录
→ 显示结算界面
```

### 示例三 集齐六把武器

```text
升级选择武器
→ PlayerShooter.EquipWeapon
→ EquippedCount 变为 6
→ BossEncounter.Update 检测条件
→ Summon 小首领
→ BossDefeated
→ 金色六选二
→ Summon 最终首领
→ GameManager.WinRun
```

### 示例四 Boss 选择下一次行动

```text
BossAgent.FixedUpdate
→ 当前行为为空且冷却结束
→ BuildSituation 收集距离/速度/靠边/血量
→ BossUtilityBrain.Evaluate
→ 六个 BossBehavior 各自过滤冷却并评分
→ 对上一次行为扣重复惩罚
→ 选择最高分
→ BossAgent 调用行为 Begin
→ 行为 Tick 移动、显示预警、从 BossHazard 池取对象
→ 行为完成，进入 recovery 后再次决策
```

## 十七、面试时应该怎样介绍架构

可以先说：

> 项目由游戏流程、玩家、武器、敌人、升级、对象池和 UI 设置几个模块组成。GameManager 管理整局生命周期，静态数值由 ScriptableObject 配置，运行时组件只维护当前状态。生命、经验和击杀使用事件通知 UI 与流程系统，高频的子弹、敌人、经验物和出生预警通过对象池复用。

面试官追问时，再选择一条调用链深入，不要一次罗列所有类名。

## 十八、当前不足和后续方向

当前不足：

- Boss 的权重和导演参数已经可调，但仍需要更多真实试玩来调平；
- 普通敌人仍是规则驱动，没有感知、包抄或群体协同；
- 动态难度只影响刷怪，不改变敌人个体战术；
- 只有一张竞技场地图；
- 测试能证明冷却、分数方向和边界，不能证明最终手感；
- Android v1.4 仍需真机验收。

后续计划：

- 依据试玩记录微调 Boss 距离权重和恢复窗口；
- 为普通敌人增加轻量感知或编队行为，但不引入无法解释的大型框架；
- 增加对局数据导出，辅助调参而不是替代设计；
- 若继续扩展，再考虑第二张地图和更多首领行为。

## 十九、以后每次更新本文件的格式

每个新需求都追加以下内容：

```text
需求是什么
玩家能看到什么变化
为什么采用这个方案
新增或修改了哪些类
从输入到结果的调用链
对象池和异常状态如何处理
增加了什么测试
人工如何验收
Git 提交和版本号
面试时如何解释
```

阅读后不理解的地方直接指出章节、类名或代码，不需要一次把整份文档全部背下。

## 二十、v1.4 智能首领与动态战斗系统总览

`2026-10-27` 完成并验证 v1.4 的代码主体。它不是简单增加更多技能，而是把“固定轮播”和“只按时间加速刷怪”改成可解释的运行时决策。

### 20.1 玩家能看到的变化

- Boss 不再固定“弹幕 → 区域攻击 → 冲刺”循环，而会根据距离、玩家速度、靠边程度和自身血量选择行为。
- 玩家远离时，Boss 更倾向追击或预测冲刺；玩家靠边时，区域轰炸分数明显提高。
- Boss 半血后解锁环形弹幕，但不能连续滥用，仍受冷却和重复惩罚。
- 普通刷怪会根据生命、近期受伤、击杀速度、武器数和场上人数调整节奏、批次、上限和敌人组合。
- 玩家残血且刚承受大量伤害时，会进入短暂停止新增刷怪的恢复窗口。

### 20.2 为什么选择 Utility AI，而不是行为树或每帧随机

这个项目需要的是“多个行为都合理时，按战场状态选一个最合适的”，并且要能在面试中解释每一项分数。Utility AI 的优点是：

- 每个行为独立评分，新增行为不需要改写整棵行为树；
- 冷却、血量门槛和重复惩罚可以在选择前过滤或统一扣分；
- 决策结果能完整输出候选分数，便于调试和自动测试；
- 不使用随机数，平局按固定顺序处理，因此同一局面可复现。

没有采用 ML-Agents 或运行时大语言模型，因为当前瓶颈是决策可解释性和手感，而不是模型容量。引入训练框架会扩大调试成本，却不一定让玩家感到更聪明。

### 20.3 改动边界

- 保留 `BossEncounter` 的小首领 → 金色六选二 → 最终首领闭环。
- 保留 `BossHazard` 对象池和所有预警可躲避规则。
- 保留玩家伤害、武器和升级数值，导演不暗中修改玩家输出。
- 所有动态参数有上下限；恢复窗口有持续时间和再次触发冷却。
- 不修改 3D《迷宫逃生》。

## 二十一、Boss Utility AI 的类职责与调用链

### 21.1 数据与情境

`BossSituation` 是只读快照，包含：

- Boss 与玩家的位置；
- 玩家刚体速度；
- Boss 生命比例；
- 玩家和 Boss 的 `EdgeProximity`（0 场地中心，1 贴边）；
- 当前时间、是否最终首领。

`BossDecisionConfig` 是 ScriptableObject，资源位于 `Assets/Resources/Combat/BossDecisionConfig.asset`。它保存六条 `BossBehaviorTuning`，每条包含基础权重、最小/理想/最大距离、冷却、前摇、动作后恢复时间，以及最终首领半血时的前摇缩短比例。共享参数包括重复惩罚、最大重复惩罚和重复记忆时间。

资源缺失或数组为空时，`BossAgent` 会使用 `CreateDefault()` 的代码默认值，保证旧场景不会因为漏挂资源而无法战斗。

### 21.2 六个行为类

| 类 | 评分关注点 | 执行结果 |
|---|---|---|
| `BossApproachBehavior` | 距离越远分数越高 | 以高于普通追击的速度接近 0.75 秒 |
| `BossPredictiveDashBehavior` | 中距离、玩家速度越高越危险 | 锁定预测方向、显示瞄准线、前摇后冲刺 |
| `BossFanVolleyBehavior` | 中距离、玩家越稳定分数越高 | 扇形弹幕；最终首领为 7 发 |
| `BossZoneBombardmentBehavior` | 玩家靠边显著加分、低速小幅加分 | 延迟范围危险；最终首领追加两侧区域 |
| `BossRingBarrageBehavior` | 半血才可评分，近中距离更强 | 环形弹幕；最终首领弹量更高 |
| `BossRepositionBehavior` | 玩家贴脸且 Boss 靠边时加分 | 向侧后方移动到新的边缘位置 |

每个行为继承 `BossBehavior`，提供：

- `IsEligible`：血量门槛等合法性；
- `Evaluate`：纯评分；
- `Begin/Tick/Cancel`：执行和终止；
- `MarkStarted/CooldownRemaining`：冷却状态。

评分公式可以概括为：

```text
可用 = 已启用 && 满足行为门槛 && 冷却结束
原始分 = 基础权重 × 距离偏好 × 情境修正
最终分 = 原始分 − min(重复惩罚 × 连续次数, 最大重复惩罚)
选择最终分最高者；平局按固定注册顺序
```

冷却在行为开始时记录，行为完成后再等待该行为的 `Recovery` 才进入下一次决策，避免“刚结束就无缝再接同一个动作”。

### 21.3 决策器与执行器

`BossUtilityBrain` 只做三件事：

1. 遍历六个行为，判断是否合法和是否冷却；
2. 计算原始分、重复惩罚和最终分；
3. 返回 `BossDecision`，其中保留全部 `BossBehaviorScore` 供调试。

`BossAgent` 负责：

- 用 `BuildSituation` 组装快照；
- 在空闲时调用 `BossUtilityBrain.Evaluate`；
- 调用选中行为的 `Begin/Tick`；
- 提供移动、预测位置、瞄准线、环形预警、`ArmZone/FireHazard` 和对象池获取；
- 玩家死亡、Boss 死亡、组件禁用时取消行为并释放危险对象。

`BossHazard` 仍只负责范围延迟伤害和直线弹丸，不参与“选择什么技能”。

### 21.4 一次完整调用链

```text
BossAgent.FixedUpdate
→ 检查玩家/Boss 存活和接触伤害
→ 若没有活动行为，检查 nextDecisionAt
→ BuildSituation
→ BossUtilityBrain.Evaluate
   ├─ 过滤冷却和半血门槛
   ├─ 六个行为分别 Evaluate
   ├─ 应用重复惩罚
   └─ 返回最高分与全部候选分数
→ BossBehavior.MarkStarted
→ BossBehavior.Begin
→ 后续 FixedUpdate 持续 Tick
   ├─ MovePosition / MoveInDirection
   ├─ ShowAim / ShowRing / ArmZone / FireHazard
   └─ 到时完成
→ CompleteActiveBehavior
→ 取消预警显示，等待 Recovery，再决策
```

### 21.5 异常与生命周期

- 玩家或 Boss 死亡：`FixedUpdate` 提前返回，`CancelActiveBehavior()` 取消当前行为。
- Boss 死亡：`Defeated()` 释放所有 `BossHazard`，关闭瞄准线和环形预警，再通知 `BossEncounter`。
- 暂停：`Time.timeScale = 0`，`FixedUpdate` 不推进，行为不会在暂停中完成。
- 决策没有可用行为：等待 0.2 秒，而不是强行选一个冷却技能或随机技能。
- 配置损坏：使用代码默认配置；不会静默禁用 Boss。

## 二十二、有边界的动态战斗导演

### 22.1 输入

`CombatDirector` 读取：

- 玩家当前生命比例；
- 最近 3 秒实际受到的伤害比例；
- 最近 8 秒击杀速率；
- 已装备武器数量；
- 当前存活敌人数和当前敌人上限；
- 战斗阶段由 `EnemySpawner` 是否启用间接决定，首领战停止普通刷怪。

`Health` 新增 `Damaged` 事件，只报告实际扣除量（已计入护甲），因此导演不会把被护甲挡掉的伤害当成压力。

### 22.2 压力模型

`CombatDirectorModel` 是纯 C# 类，可脱离场景测试。目标压力由以下项相加后夹在 `[0,1]`：

```text
基础压力
+ 生命越健康越高
+ 武器越多越高
+ 击杀率超过基线时提高
− 场上敌人越接近上限越低
− 最近受伤越高越低
```

当前压力不会瞬间跳变，而是按 `pressureSmoothing` 每秒向上或向下靠近目标。所有输出都从压力映射：

| 参数 | 下限 | 上限 |
|---|---:|---:|
| 刷怪间隔 | 0.55 秒 | 1.50 秒 |
| 场上敌人数 | 10 | 28 |
| 单批生成数 | 1 | 3 |
| 难度等级 | 0 | 4 |

玩家表现强时，Dasher/Berserker 的权重倍率随压力提高；玩家进入恢复时，高级敌人倍率下降，Chaser 倍率提高。

### 22.3 恢复窗口

触发条件必须同时满足：

```text
生命比例 ≤ 0.35
最近 3 秒受伤比例 ≥ 0.18
距离上次恢复结束已超过 7 秒冷却
```

满足后：

- 进入 3.5 秒恢复；
- `SpawnsSuppressed = true`，停止新增刷怪；
- 批次为 0，敌人上限降到最小值；
- 3.5 秒结束后恢复，但 7 秒内不能再次触发。

恢复窗口不是无敌，也不是暗中削弱敌人。已经出现在场上的敌人仍会战斗，已经落地的出生预警仍可能生成敌人；它只停止新的刷怪压力。

### 22.4 导演调用链

```text
Health.Damaged / EnemyController.DiedGlobally
→ CombatDirector 记录带时间戳的伤害和击杀
→ Update 清理 3 秒/8 秒窗口
→ 组装 CombatDirectorInput
→ CombatDirectorModel.Step 平滑压力、判断恢复
→ CombatDirectorOutput
→ EnemySpawner.Update 读取间隔、批次、上限
→ ClampBatchToCapacity 检查剩余容量
→ 从预警池生成 0-3 个标记
→ 权重选择时乘 EnemyWeightMultiplier
```

### 22.5 为什么导演不会无限加压

- 压力本身被限制在 `[0,1]`；
- 刷怪间隔有最快和最慢边界；
- 场上人数有上限；
- 残血和近期伤害会把压力拉低；
- 恢复窗口有持续时间和冷却；
- 不直接修改玩家伤害、敌人伤害或玩家生命。

这套设计的目标是“节奏有反应”，不是“让系统装作会玩”。

## 二十三、决策调试、测试与人工验收

### 23.1 开发决策面板

在 Editor 或 Development Build 中，`GameManager.Awake` 自动创建 `CombatDebugOverlay`。它显示：

- 当前 Boss 行为和上一次行为；
- 按最终分排序的前四名候选行为、分数、剩余冷却和重复标记；
- 导演压力、难度等级、实际刷怪间隔、上限、批次；
- 最近伤害比例、恢复倒计时和存活敌人数；
- `F3` 显示/隐藏。

面板只读，不参与决策，也不改变游戏数值。

### 23.2 自动测试覆盖

当前 PlayMode `18/18`。v1.4 新增测试分为两类：

- Boss：冷却中的技能不可选、重复行为扣分、远距离偏好追击、靠边提高区域轰炸、半血解锁环形弹幕、六个行为均可执行完成、Boss 死亡释放危险并停止决策。
- 导演：压力与输出边界、重伤恢复、恢复冷却、敌人数上限批次、真实场景重伤恢复。

原有六武器 → 小首领 → 金色六选二 → 最终首领 → 胜利流程仍在同一套 PlayMode 中回归。

### 23.3 人工验收清单

自动测试证明规则，不证明手感。发布前应在 Windows 和 Android 上分别检查：

1. 开局按 `F3`（Windows）能看到 Boss 和导演信息；Android 开发包默认显示。
2. 打完整一局，观察 Boss 是否会在远距离追击、中距离冲刺/弹幕、玩家靠边时区域轰炸之间变化，而不是固定循环。
3. 把 Boss 打到半血，确认环形弹幕出现，但不会连续重复到无法躲避。
4. 故意让玩家残血并承受一次重击，确认刷怪短暂停止，恢复后不会立刻再次停刷。
5. 观察高表现时敌人组合和批次是否适度提高，同时场上人数不超过上限。
6. 完成六武器、小首领、金色六选二、最终首领和胜利结算。
7. 确认暂停、死亡、Boss 死亡后没有残留预警或继续决策。

未完成的真机项目必须明确写入结果，不能用自动截图代替作者试玩。

### 23.4 当前验证状态

- PlayMode `18/18` 通过，XML 证据在 `docs/test-results/v1.4.0/PlayMode-results.xml`。
- 发布边界 `30/30` 通过。
- Windows 候选包构建 `0 error`，受控首领画面和战斗冒烟通过。
- Android 候选 APK 已构建成功：约 51.7 MB，0 error、1 warning；构建耗时 3 分 42 秒，versionCode 5。
- APK 使用本地 Android Debug 证书签名，不是生产签名；当前没有连接设备，未安装，因此不把构建成功写成真机通过。
- Windows 自动首领练习从 42 点生命开始，使用固定自动规避，在约 23 秒被一次范围攻击击杀。日志已保留为 `NEON_BOSS_PRACTICE_FAIL`，不能写成人工通关。
- WebGL 构建已尝试，但在本地 Player DAG 阶段停滞且没有产物，未计入通过。
- Draft PR #17 已创建，但尚未合并或发布。
- 作者 Windows/Android 手动验收仍是发布前待办。

这意味着代码主体和工程证据已经完成，但“手感是否合适”和“真机是否可玩”没有被自动化替代。

## 二十四、Git 记录与面试讲法

### 24.1 Git 记录

- 基线文档提交：`517dd0e docs: explain Neon Arena development architecture`
- Boss 行为拆分与 Utility AI：`f891bc6 feat: replace fixed boss rotation with utility behaviors`
- 动态战斗导演：`bae78f0 feat: add bounded dynamic combat director`
- 调试面板与自动测试：`8da5ada test: expose boss and director decisions for debugging`
- v1.4.0 版本号、边界验证与发布冒烟：`3a382d9 chore: prepare v1.4.0 release verification`
- 发布候选文档与证据：`d0e616a docs: record v1.4 AI design tests and release evidence`
- 功能分支：`feature/intelligent-combat-ai`
- Draft PR：<https://github.com/ff1333/NeonArena/pull/17>

合并提交、Release 链接和作者验收结果在发布完成后补记到总控文件。

### 24.2 60 秒面试讲法

> 这个版本要解决两个问题：Boss 技能固定轮播，以及刷怪只随时间加速。Boss 侧我把冲刺、弹幕、范围轰炸、环形弹幕、追击和重新站位拆成独立行为，每个行为都能评分、执行和冷却。BossAgent 每轮收集距离、玩家速度、靠边程度和血量，BossUtilityBrain 先过滤冷却和半血门槛，再算距离偏好和重复惩罚，最后选最高分。决策层不直接生成伤害，移动和对象池仍由 BossAgent/BossHazard 执行。刷怪侧增加了 CombatDirector，用生命、近期伤害、击杀率、武器数和场上人数算一个带上下限的压力值，只调整间隔、批次、人数上限和敌人权重；残血重伤会进入有限恢复窗口。自动测试覆盖了冷却、重复惩罚、靠边偏好、半血解锁、人数上限和恢复冷却，原有首领闭环也保留。

### 24.3 常见追问

**为什么不直接用行为树？**

行为树适合明确的优先级和顺序，但这里多个行为在不同距离、速度、场地下都可能合理。Utility AI 让每个行为独立表达“现在为什么想做这件事”，还能输出分数做调试。

**怎么避免 Boss 一直放同一个技能？**

冷却先过滤，重复惩罚再扣分；连续重复时惩罚递增，但有上限。平局按固定顺序处理，不靠随机掩盖问题。

**导演会不会偷偷降低玩家伤害？**

不会。导演只改变刷怪间隔、批次、敌人上限和权重。玩家的伤害、敌人的伤害和生命规则没有被导演修改。

**怎么证明不是随机？**

同一 `BossSituation`、同一冷却状态和同一配置下，评分和选择是确定的；测试直接构造情境断言选择方向和分数变化。

**恢复窗口会不会让游戏太简单？**

它有 3.5 秒持续时间、7 秒再次触发冷却，而且只停止新增刷怪，不清理场上敌人。它的目标是给玩家一个喘息的决策空间，不是重置战斗。

具体阶段状态、测试结果和发布进度记录在 [README](../README.md)、[PR #17](https://github.com/ff1333/NeonArena/pull/17) 与 [v1.4 验证记录](test-results/v1.4.0/README.md)。
