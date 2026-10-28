# v1.4.0 人工验收清单

状态：**未执行**。本文件用于作者试玩，不是通过记录。

自动测试和构建已经完成，但仍需要用真实操作确认 Boss 行为是否可读、恢复窗口是否自然、Android 触屏是否正常。

## 候选包

| 平台 | 路径 | 校验 |
|---|---|---|
| Windows | `Builds/Release/NeonArena-v1.4.0-Windows.zip` | SHA-256 `44660facd2713f7fb849d5d4ed49507fafc459169a0daade363aee2991848f82` |
| Android | `Builds/Android/v1.4.0/NeonArenaRebuild-v1.4.0.apk` | SHA-256 `0a08573111497d5d22d42175f24e78bc4c3174a4e3c3a1d7160f5de2f6fc4e42` |

Android APK 使用本地 **Android Debug** 证书签名，不是生产签名。当前机器没有连接 Android 设备。

## Windows 检查

1. 解压 `NeonArena-v1.4.0-Windows.zip`，运行 `NeonArenaRebuild.exe`。
2. 完成一局正常流程：选初始武器 → 升级 → 六武器 → 小首领 → 金色六选二 → 最终首领 → 胜利。
3. 观察 Boss 是否在追击、预测冲刺、扇形弹幕、区域轰炸、半血环形弹幕和换位之间变化。
4. 玩家远离、贴近边缘、Boss 半血时分别观察行为倾向。
5. 故意让玩家残血并承受一次重击，确认新增刷怪暂停约 3.5 秒；持续保持低血，确认不能反复触发。
6. 将生命恢复到 55% 以上，再次跌到 35% 以下并承受重击，确认恢复重新可用。
7. 后期维持低生命一段时间，确认刷怪不会长期退回开局节奏。
8. 确认暂停、死亡、Boss 死亡后没有残留预警或继续攻击。
9. 记录不同武器构筑下最终 Boss 是否仍可完成。

调试面板只在 Editor 或 Development Build 中存在。普通 Windows 候选包按 `F3` 不会显示，这是预期行为。

## Android 检查

1. 在真机安装 APK（需要允许调试签名安装）。
2. 检查横屏、安全区和触屏摇杆。
3. 完成一局，确认选武器、升级、暂停、Boss 战和结算。
4. 记录设备型号、Android 版本、是否掉帧/发热/触控漂移。

## 调试面板（Editor / Development Build）

在 Unity Editor 中打开 `Assets/Scenes/Main.unity`，进入 Play Mode，按 `F3`：

- 当前 Boss 行为、上一行为；
- 前四名候选行为分数、冷却、重复标记；
- 导演当前压力、进程压力、最低压力、难度等级、实际刷怪间隔、上限和批次；
- 最近伤害、恢复倒计时、恢复是否已武装和存活敌人数。

## 结果记录

把结果补到 `docs/test-results/v1.4.0/README.md`：

```text
日期：
Windows：通过 / 失败 / 未执行；问题：
Android：设备型号 / 系统版本 / 通过 / 失败 / 未执行；问题：
Boss 行为是否可读：
恢复窗口是否自然：
是否发现阻塞发布的问题：
```

只有 Windows 与 Android 都完成基本验收，才把 PR #17 从 Draft 转为 Ready，合并并创建 `v1.4.0` Release。
