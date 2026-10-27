# Neon Arena v1.4.0 — Intelligent Boss and Dynamic Combat Director

Status: **release candidate; draft release assets uploaded, not published**.

Date: 2026-10-27. The code, PlayMode tests, boundary checks, Windows candidate, Android candidate APK and an unpublished Draft Release are ready. Author Windows/Android playtesting is still required before merge and publishing the release.

## Highlights

### Boss Utility AI

The Warden and Overlord no longer cycle through three attacks by index. Each action is now an independent behavior with its own score, range preference, cooldown and recovery:

- `Pursuit`
- `Predictive Dash`
- `Fan Volley`
- `Zone Bombardment`
- `Ring Barrage` (unlocked below 50% boss health)
- `Reposition`

`BossUtilityBrain` filters cooling-down actions, applies a repeat penalty and selects the highest deterministic score. `BossDecisionConfig` exposes base weights, range bands, cooldowns, windups and repeat rules as a ScriptableObject.

### Bounded Dynamic Combat Director

`CombatDirector` reads player health, damage taken in the last 3 seconds, kill rate over 8 seconds, weapon count, active enemies and the current cap. It produces only bounded spawn parameters:

| Parameter | Allowed range |
|---|---|
| Spawn interval | 0.55–1.50 s |
| Enemy cap | 10–28 |
| Batch size | 1–3 |
| Difficulty tier | 0–4 |

When the player is at or below 35% health and has just taken heavy damage, new spawns pause for 3.5 seconds. The window has a 7-second retrigger cooldown. It does not clear existing enemies and does not secretly change player or enemy damage.

### Debug and testability

- Development overlay (`F3` in Editor or Development Build) shows the current boss action, candidate scores, cooldowns, repeat flags, pressure, tier, interval, cap, batch and recovery timer.
- PlayMode coverage increased from 6 to 18 tests.
- Release boundary verification increased from 24 to 30 checks, including AI config presence, six valid boss actions, scoring availability, director bounds and spawn-cap clamping.

## Compatibility

- Six-weapon loadout, mini-boss trigger, golden two-of-six rewards, final boss and victory flow are unchanged.
- Existing object pools and `BossHazard` projectiles/zones are reused.
- 3D project *Maze Escape* is not modified by this release.

## Verification

- PlayMode: `18/18 PASS`
- Release boundary: `30/30 PASS`
- Windows automated full run: victory at 880s (level 17, six weapons, 48/220 HP)
- Windows build: 0 errors; boss/combat smoke PASS
- Android candidate APK: built, debug-signed, no device test
- Unity: `6000.3.18f1`
- Platform build and device evidence: see [v1.4.0 verification](../devlogs/22-v1.4.0-build-verification.md)

## Known limits

- Automatic tests prove rules and bounds, not final game feel.
- Boss and director weights still need broader manual playtesting.
- Windows candidate runtime smoke passes; the fixed autopilot boss practice failed at 42 starting HP and is recorded as a limitation, not a manual clear.
- Android v1.4.0 candidate APK builds successfully with the local Android Debug certificate, but no device is connected; installation, production signing and device verification are not done.
- WebGL build was attempted but stalled in the local Player DAG stage and produced no artifact.
- The game remains a single-arena, single-player prototype without progression saving or online play.
