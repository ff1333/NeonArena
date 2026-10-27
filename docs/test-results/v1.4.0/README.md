# v1.4.0 验证证据

日期：2026-10-27。执行者：AI 辅助工具 + Unity Test Framework，不是作者手动试玩记录。

## 自动逻辑验证

- `PlayMode-results.xml`：18 项通过，0 失败，总耗时 38.47 秒。
- 发布边界：30 项通过，0 失败，见 [构建验证日志](../../devlogs/22-v1.4.0-build-verification.md)。
- Unity：`6000.3.18f1`。
- 分支：`feature/intelligent-combat-ai`。

覆盖范围：

- 原六武器 → 小首领 → 两级奖励 → 金色六选二 → 最终首领 → 胜利闭环；
- Boss 六个行为可独立启动并完成；
- 冷却过滤、重复惩罚、远距离追击偏好、靠边区域轰炸偏好、半血环形弹幕；
- Boss 死亡释放危险并停止决策；
- 导演压力/间隔/上限/批次边界；
- 残血重伤恢复窗口、恢复冷却、敌人上限；
- 设置持久化和预警躲避。

## 独立程序冒烟

Windows 候选包：

- `NEON_CAMPAIGN_SMOKE_PASS stage=MiniFight`：六武器、首领生成和首领画面；
- `NEON_POLISH_FLOW_PASS weapons=1 enemyStyles=3 pooledTrails=clear`：开局武器、三类敌人外观和投射物轨迹复用；
- `NEON_PRESENTATION_SMOKE_PASS`：首领/战斗截图成功；
- `NEON_FULL_RUN_VICTORY time=880.0 level=17 weapons=6 hp=48/220`：自动策略完成六武器、小首领、金色奖励、最终首领和胜利，日志见 [Windows-fullrun.txt](Windows-fullrun.txt)。

自动完整通关证明流程可完成，但仍不是人工试玩。它消耗了接近 900 秒的内部预算，且不能证明可读性、手感和 Android 触屏。

截图来自受控装配，不是完整手动通关：

- `native/v1.4.0-boss.png`
- `native/v1.4.0-combat.png`

## 自动首领练习的边界

`-neonPortfolioShot bosspractice` 使用自动化规避脚本，并从 42 点生命开始小首领战。v1.4.0 候选包在约 23 秒时被一次范围攻击击杀，日志为 `NEON_BOSS_PRACTICE_FAIL`。

这不能直接解释为“Boss 无法击败”：该脚本不是人类玩家，起始生命也低于正常满血；但它说明新 AI 已经能命中固定的自动规避路线，Boss 权重和攻击命中后的惩罚仍需要真实试玩调平。不能把这次失败当作人工通关证据，也不能把它隐藏为通过。

## 构建结果

| 平台 | 构建 | 运行时检查 | 错误 | 备注 |
|---|---|---|---:|---|
| Windows | PASS | 首领与战斗冒烟 PASS | 0 | 全量构建 1 条已知 Sprite Tiling 导入提示；增量总结 0 警告 |
| Android | PASS | 未执行 | 0 | APK 51,728,735 bytes；1 warning；没有连接设备 |
| WebGL | BLOCKED | 未执行 |  | 已尝试构建，在本地 Player DAG 阶段停滞且无产物 |

## Git / PR

- 分支：`feature/intelligent-combat-ai`
- 发布候选 PR：<https://github.com/ff1333/NeonArena/pull/17>（Draft）
- 未合并；已建立不公开的 Draft Release（id `425471979`），上传 Windows ZIP、Android APK 和 SHA-256 校验文件。
- 未发布公开的 `v1.4.0` GitHub Release；等待作者试玩与发布决定。
- Windows 候选包：`Builds/Release/NeonArena-v1.4.0-Windows.zip`（53,514,909 bytes）。
- Android 构建证据：[build-summary](Android-build-summary.txt)、[SHA-256](Android-SHA256SUMS.txt)、[manifest/signature](Android-verify.txt)。
- Android 候选 APK：`Builds/Android/v1.4.0/NeonArenaRebuild-v1.4.0.apk`（51,728,735 bytes，本地构建产物，未提交到 Git）。
- 两个候选包的发布校验和：[SHA256SUMS.txt](SHA256SUMS.txt)。
- APK 为 **Android Debug** 证书签名，不是生产签名；当前没有连接设备，未安装、未真机测试。

## 作者真机验收

v1.4.0 的 Windows 和 Android 手动试玩尚未由作者完成。Android 构建已成功，但当前没有连接设备，安装和真机检查未执行。发布说明、README 和本文件不得把自动截图、构建成功或自动首领练习写成作者真机验收。

需要作者补做的检查：

1. Windows 完整一局，观察 Boss 行为变化、半血环形弹幕和结算；
2. Android 安装、触屏移动、暂停和首领战；
3. 低生命时恢复窗口是否可感知，且不会让节奏突然中断过久；
4. 不同武器构筑下 Boss 战是否仍可完成。
