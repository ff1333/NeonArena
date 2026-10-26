# 霓虹竞技场完整开发解释

这是一份持续更新的学习文档，用来解释《霓虹竞技场》从启动到通关的完整代码流程。它不要求先记住所有脚本，而是帮助你建立“玩家操作发生后，哪些对象、组件和数据依次工作”的整体认识。

当前解释基于正式版 `v1.3.0`。后续新增 Boss Utility AI、动态战斗导演或其他功能时，继续更新本文件，不再另起一套互相割裂的说明。

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

核心代码可以分为七组。

| 系统 | 主要脚本 | 负责什么 |
|---|---|---|
| 游戏流程 | `GameManager`、`BossEncounter` | 开始、暂停、死亡、胜利和首领阶段 |
| 玩家 | `PlayerMovement`、`Health`、`PlayerProgress` | 移动、生命、经验和等级 |
| 武器 | `PlayerShooter`、`WeaponDefinition`、`Projectile` | 装备、索敌、冷却、发射和伤害 |
| 敌人 | `EnemySpawner`、`EnemyController`、`EnemyDefinition` | 刷怪、行为、受伤、死亡和经验掉落 |
| 升级 | `UpgradeController`、`PlayerUpgradeApplier`、`PlayerUpgradeData` | 生成升级选项并应用效果 |
| 性能 | `GameObjectPool`、`PoolMember`、`EnemyRegistry` | 复用高频对象并维护敌人集合 |
| 表现与平台 | `HudController`、`CombatFeedback`、`PortfolioSettings`、移动端脚本 | UI、反馈、设置、本地化和触屏输入 |

可以把整体关系理解为：

```text
GameManager 管理整局生命周期
├─ PlayerMovement 读取输入并移动
├─ PlayerShooter 根据武器配置自动攻击
├─ EnemySpawner 根据时间和权重生成敌人
├─ PlayerProgress 接收经验并请求升级
├─ UpgradeController 暂停游戏并显示选项
└─ BossEncounter 在六武器后接管战斗阶段
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

### 9.2 EnemySpawner

刷怪器根据存活时间计算当前间隔：

```text
startingInterval - 存活时间 × 每秒缩短量
```

再使用 `Mathf.Max` 限制最小间隔，避免刷怪无限加快。

敌人类型通过权重随机选择，并受解锁时间限制。高级敌人不会在开局十几秒就出现。

出生位置在整个竞技场内随机选择，但必须与玩家保持最小距离。找不到合适位置时，选择距离玩家最远的角落。

正式生成前先从警告对象池获取红色预警标记，等待约 1.5 秒后再生成敌人。

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

### 当前 Boss 决策方式

`BossAgent` 当前使用 `attackIndex % 3` 按顺序选择：

- 弹幕；
- 区域攻击；
- 冲刺。

它能够预判玩家速度，也有半血强化，但技能顺序仍是固定的。后续 Utility AI 会把“固定顺序”改成“根据战场状态评分选择”。

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

当前 PlayMode 测试为 `6/6`，重点覆盖：

- 六武器触发小首领；
- 小首领奖励两级；
- 金色六选二；
- 最终首领和胜利；
- 设置持久化。

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

## 十七、面试时应该怎样介绍架构

可以先说：

> 项目由游戏流程、玩家、武器、敌人、升级、对象池和 UI 设置几个模块组成。GameManager 管理整局生命周期，静态数值由 ScriptableObject 配置，运行时组件只维护当前状态。生命、经验和击杀使用事件通知 UI 与流程系统，高频的子弹、敌人、经验物和出生预警通过对象池复用。

面试官追问时，再选择一条调用链深入，不要一次罗列所有类名。

## 十八、当前不足和后续方向

当前不足：

- Boss 技能仍按固定顺序选择；
- 普通敌人行为以规则状态为主；
- 动态难度主要依赖时间和固定配置；
- 只有一张竞技场地图；
- 自动测试数量偏少。

后续计划：

- Boss Utility AI；
- 有边界的动态战斗导演；
- 增加决策调试信息；
- 增加决策和阶段测试；
- 使用对局数据辅助调整权重，但不把测试作为项目主线。

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
