# itch.io 发布页文案与设置

本文件用于创建 Restricted 页面时直接逐项填写。公开链接产生前，不在 README 中编造链接。

## 基本设置

- Title: `Neon Arena Rebuild`
- Project URL: `neon-arena-rebuild`
- Classification: `Games`
- Kind of project: `HTML`
- Release status: `Released`
- Pricing: `No payments`
- Visibility: 首次上传选择 `Restricted`
- Viewport: `960 x 540`
- Mobile friendly: 当前 WebGL 页面未作为手机浏览器版本验收，不勾选

## Short description

```text
A Unity 6 / C# 2D survival shooter rebuilt as a portfolio project, featuring data-driven weapons and enemies, six weapon slots, object pooling, and multiplatform builds.
```

## 页面正文

```markdown
# Neon Arena Rebuild

Neon Arena Rebuild is a 2D survival shooter rebuilt with Unity 6 and C# as a personal portfolio project.

Move through a bounded arena while independent weapons automatically acquire targets. Defeated enemies drop experience, and each level offers one of three upgrades. Every third level prioritizes weapon choices, with up to six equipped weapon slots.

## Controls

- WASD / Arrow keys: Move
- Enter: Open the starting weapon selection
- Esc: Pause / Resume
- R: Restart after game over
- Mouse: UI buttons and upgrade choices

## Features

- Pistol, SMG and Laser starting choice with independent range, fire rate and cooldown
- Six total weapon slots with unrestricted same-type specialization
- Chaser, Dasher and Berserker enemy behaviors
- Distinct experience pickup shapes for each enemy type
- ScriptableObject-driven weapon, enemy and upgrade data
- Object pools for projectiles, enemies, experience pickups and spawn warnings
- Upgrade filtering, arena bounds, camera follow and combat feedback
- Windows, WebGL and Android build pipeline

Source code, development logs and Windows/Android downloads:
https://github.com/ff1333/NeonArena

This is a personal learning and portfolio rebuild, not a commercial release.
```

## 上传文件

```text
D:\software\documents\unity_games\projects\learning\NeonArenaRebuild\Builds\Packages\v1.1.0\NeonArenaRebuild-WebGL-v1.1.0.zip
```

上传完成后勾选 `This file will be played in the browser`。如果实际 zip 位于其他目录，必须先确认压缩包根目录直接包含 `index.html`，不能盲目照抄路径。

## Restricted 验收

- [ ] 页面首次加载完成
- [ ] Start 可点击
- [ ] WASD 和方向键可移动
- [ ] 自动射击、击杀和经验拾取正常
- [ ] 升级卡片可点击且文字完整
- [ ] Pause / Resume 正常
- [ ] Game Over / Restart 正常
- [ ] 浏览器开发者工具 Console 没有持续游戏错误
- [ ] 使用未登录窗口通过 Restricted 分享链接完成一次测试

以上全部通过后，才将 Visibility 从 `Restricted` 改为 `Public`。公开后再次使用未登录窗口检查，并把真实链接补到根目录 `README.md` 和课程总清单。
