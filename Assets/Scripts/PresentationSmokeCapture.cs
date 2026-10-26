using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public sealed class PresentationSmokeCapture : MonoBehaviour
{
    private static readonly FieldInfo CurrentChoicesField =
        typeof(UpgradeController).GetField(
            "currentChoices",
            BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo HazardZoneField =
        typeof(BossHazard).GetField(
            "zone",
            BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo HazardRadiusField =
        typeof(BossHazard).GetField(
            "radius",
            BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo HazardVelocityField =
        typeof(BossHazard).GetField(
            "velocity",
            BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo BossLockedDirectionField =
        typeof(BossAgent).GetField(
            "lockedDirection",
            BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo BossAttackIndexField =
        typeof(BossAgent).GetField(
            "attackIndex",
            BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo BossDashUntilField =
        typeof(BossAgent).GetField(
            "dashUntil",
            BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo EnemyPreparingDashField =
        typeof(EnemyController).GetField(
            "isPreparingDash",
            BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo EnemyDashingField =
        typeof(EnemyController).GetField(
            "isDashing",
            BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo EnemyDashDirectionField =
        typeof(EnemyController).GetField(
            "dashDirection",
            BindingFlags.Instance | BindingFlags.NonPublic);

    private bool portfolioShotFailed;
    private bool emergencySteeringActive;
    private Vector2 previousRunDirection = Vector2.right;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        var args = Environment.GetCommandLineArgs();
        if (Array.IndexOf(args,"-neonSmoke") >= 0 ||
            Array.IndexOf(args,"-neonPortfolioVideo") >= 0)
        {
            Application.runInBackground=true;
            new GameObject("Presentation Smoke").AddComponent<PresentationSmokeCapture>();
        }
    }

    private IEnumerator Start()
    {
        var args = Environment.GetCommandLineArgs();
        if (Array.IndexOf(args,"-neonPortfolioVideo") >= 0)
        {
            yield return RunPortfolioShot(ReadArgument(args,"-neonPortfolioShot"));
            yield break;
        }
        var captureIndex = Array.IndexOf(args,"-screenshot");
        if (captureIndex < 0 || captureIndex + 1 >= args.Length) { Application.Quit(2); yield break; }
        yield return new WaitForSecondsRealtime(1f);
        Application.runInBackground=true;
        var previousLanguage=PortfolioSettings.Chinese;
        var hadBestTime=PlayerPrefs.HasKey("BestTime");var previousBestTime=PlayerPrefs.GetFloat("BestTime");
        var hadBestKills=PlayerPrefs.HasKey("BestKills");var previousBestKills=PlayerPrefs.GetInt("BestKills");
        PortfolioSettings.SetLanguage(Array.IndexOf(args,"-english")<0);
        yield return null;
        if(Array.IndexOf(args,"-settings")>=0) { PortfolioSettingsMenu.Instance.Open();yield return null; }
        var bossCase=Array.Exists(args,arg=>arg is "-neonBoss" or "-neonRewards" or "-neonFinal" or "-neonVictory");
        if(bossCase)
        {
            var manager=FindFirstObjectByType<GameManager>();manager.StartRun();yield return null;yield return null;
            GameObject.Find("UpgradeButton1").GetComponent<Button>().onClick.Invoke();yield return null;
            var weapons=FindFirstObjectByType<PlayerShooter>();weapons.enabled=false;FindFirstObjectByType<EnemySpawner>().enabled=false;
            var definition=Resources.FindObjectsOfTypeAll<WeaponDefinition>().First();
            while(weapons.EquippedCount<6)weapons.EquipWeapon(definition);
            yield return new WaitForSeconds(2.3f);
            var encounter=manager.GetComponent<BossEncounter>();
            if(encounter.Stage!=BossStage.MiniFight)throw new InvalidOperationException("Boss trigger failed");
            if(Array.IndexOf(args,"-neonBoss")<0)
            {
                encounter.ActiveBoss.Health.TakeDamage(1000000);yield return null;
                if(Array.IndexOf(args,"-neonRewards")<0)
                {
                    encounter.ChooseReward(0);encounter.ChooseReward(2);yield return new WaitForSeconds(2.3f);
                    if(Array.IndexOf(args,"-neonVictory")>=0){encounter.ActiveBoss.Health.TakeDamage(1000000);yield return null;}
                }
            }
            if(encounter.ActiveBoss!=null)
            {
                encounter.ActiveBoss.transform.position=weapons.transform.position+Vector3.up*3.2f;
                yield return new WaitForSeconds(.9f);
            }
            Debug.Log("NEON_CAMPAIGN_SMOKE_PASS stage="+encounter.Stage);
        }
        if (Array.IndexOf(args,"-neonLoadout") >= 0)
        {
            FindFirstObjectByType<GameManager>().StartRun();
            yield return null;
        }
        if (Array.IndexOf(args,"-neonGameplay") >= 0)
        {
            FindFirstObjectByType<GameManager>().StartRun();
            yield return null;
            GameObject.Find("UpgradeButton1").GetComponent<Button>().onClick.Invoke();
            yield return null;
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player.GetComponent<PlayerShooter>().EquippedCount != 1 || Time.timeScale == 0)
                throw new InvalidOperationException("Starting weapon flow failed");
            var spawner = FindFirstObjectByType<EnemySpawner>();
            spawner.enabled = false;
            var entries = (EnemySpawnEntry[])typeof(EnemySpawner).GetField("enemyTypes",BindingFlags.Instance | BindingFlags.NonPublic).GetValue(spawner);
            var pool = GameObject.Find("EnemyPool").GetComponent<GameObjectPool>();
            var xp = GameObject.Find("ExperiencePool").GetComponent<GameObjectPool>();
            for (var i = 0; i < entries.Length; i++)
            {
                var enemy = pool.Get(new Vector3(-3f + i * 3f,3.5f,0),Quaternion.identity).GetComponent<EnemyController>();
                enemy.Spawn(player.transform,xp,entries[i].Definition);
                if (enemy.GetComponent<SpriteRenderer>().sprite.name != entries[i].Definition.BehaviorType.ToString())
                    throw new InvalidOperationException("Enemy appearance mismatch");
            }
            yield return new WaitForSeconds(1f);
            player.GetComponent<Health>().TakeDamage(15f);
            CombatFeedback.Instance.ShowDamage(new Vector3(0,2,0),34,false);
            // Verify pooled trails cannot connect a reused projectile to its previous location.
            var bullets = FindObjectsByType<Projectile>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            foreach (var bullet in bullets)
            {
                if (!bullet.gameObject.activeSelf && bullet.TryGetComponent<TrailRenderer>(out var trail) && trail.positionCount != 0)
                    throw new InvalidOperationException("Inactive projectile retained a trail");
            }
            Debug.Log("NEON_POLISH_FLOW_PASS weapons=1 enemyStyles=3 pooledTrails=clear");
            if (Array.IndexOf(args,"-neonPause") >= 0)
                GameObject.Find("PauseButton").GetComponent<Button>().onClick.Invoke();
        }
        var output = args[captureIndex + 1];
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        Time.timeScale = 0f;
        yield return null;
        var camera=Camera.main;
        foreach(var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        { canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;canvas.sortingOrder+=1000; }
        foreach(var label in FindObjectsByType<LocalizedLabel>(FindObjectsSortMode.None)) label.Refresh();
        Canvas.ForceUpdateCanvases();
        var target=new RenderTexture(Screen.width,Screen.height,24);camera.targetTexture=target;camera.Render();
        var previous=RenderTexture.active;RenderTexture.active=target;
        var texture=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);texture.Apply();
        File.WriteAllBytes(output,texture.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=previous;Destroy(texture);Destroy(target);
        PortfolioSettings.SetLanguage(previousLanguage);
        if(hadBestTime)PlayerPrefs.SetFloat("BestTime",previousBestTime);else PlayerPrefs.DeleteKey("BestTime");
        if(hadBestKills)PlayerPrefs.SetInt("BestKills",previousBestKills);else PlayerPrefs.DeleteKey("BestKills");
        PlayerPrefs.Save();
        Debug.Log("NEON_PRESENTATION_SMOKE_PASS " + output);
        Application.Quit(0);
    }

    private IEnumerator RunPortfolioShot(string shot)
    {
        Application.runInBackground = true;
        PortfolioSettings.SetLanguage(true);
        yield return new WaitForSecondsRealtime(1.5f);

        switch (shot)
        {
            case "settings":
                PortfolioSettingsMenu.Instance.Open();
                yield return new WaitForSecondsRealtime(10f);
                break;
            case "loadout":
                FindFirstObjectByType<GameManager>().StartRun();
                yield return new WaitForSecondsRealtime(12f);
                break;
            case "upgrade":
                yield return RunUpgradeShot();
                break;
            case "boss":
                yield return RunBossShot(false);
                break;
            case "final":
                yield return RunBossShot(true);
                break;
            case "combat":
                yield return RunCombatShot(30f,false);
                break;
            case "advanced":
                yield return RunAuthenticAdvancedShot(66f);
                break;
            case "fullrun":
                yield return RunAuthenticFullRun(900f);
                break;
            case "bosspractice":
                yield return RunBossPractice();
                break;
            default:
                yield return new WaitForSecondsRealtime(10f);
                break;
        }

        if (portfolioShotFailed)
        {
            Application.Quit(3);
            yield break;
        }

        Debug.Log("NEON_PORTFOLIO_SHOT_PASS " + shot);
        Application.Quit(0);
    }

    private IEnumerator RunAuthenticFullRun(float timeout)
    {
        // Keep the title visible long enough for the recorder to start, then use
        // only normal game flow until the victory screen appears.
        yield return new WaitForSecondsRealtime(3f);
        FindFirstObjectByType<GameManager>().StartRun();
        yield return null;
        yield return null;
        InvokeUpgradeButton(ChooseStartingWeaponIndex());
        yield return null;

        var manager = FindFirstObjectByType<GameManager>();
        var player = GameObject.FindGameObjectWithTag("Player");
        var body = player.GetComponent<Rigidbody2D>();
        var movement = player.GetComponent<PlayerMovement>();
        var health = player.GetComponent<Health>();
        var progress = player.GetComponent<PlayerProgress>();
        var shooter = player.GetComponent<PlayerShooter>();
        var bounds = FindFirstObjectByType<ArenaBounds>();
        var upgrades = FindFirstObjectByType<UpgradeController>();
        var encounter = manager.GetComponent<BossEncounter>();
        movement.enabled = false;

        var elapsed = 0f;
        var choiceElapsed = 0f;
        var lastLevel = progress.Level;
        var lastWeapons = shooter.EquippedCount;
        var lastStage = encounter.Stage;

        Debug.Log(
            $"NEON_FULL_RUN_START level={progress.Level} " +
            $"weapons={shooter.EquippedCount} hp={health.Current:0}/{health.Max:0}");

        while (elapsed < timeout && !health.IsDead && !manager.HasWon)
        {
            elapsed += Time.unscaledDeltaTime;

            if (encounter.Stage != lastStage)
            {
                lastStage = encounter.Stage;
                Debug.Log(
                    $"NEON_FULL_RUN_STAGE stage={lastStage} time={elapsed:0.0} " +
                    $"level={progress.Level} weapons={shooter.EquippedCount} " +
                    $"hp={health.Current:0}/{health.Max:0}");
            }

            if (upgrades.IsOpen)
            {
                body.linearVelocity = Vector2.zero;
                choiceElapsed += Time.unscaledDeltaTime;
                if (encounter.Stage == BossStage.Rewards)
                {
                    if (choiceElapsed >= 1.5f)
                    {
                        var reward = encounter.RewardsSelected == 0 ? 3 : 0;
                        encounter.ChooseReward(reward);
                        choiceElapsed = 0f;
                    }
                }
                else if (choiceElapsed >= 1.15f)
                {
                    InvokeUpgradeButton(
                        ChooseUpgradeIndex(upgrades,health,shooter));
                    choiceElapsed = 0f;
                }
                yield return null;
                continue;
            }

            choiceElapsed = 0f;
            var direction = ChooseFullRunDirection(
                body.position,
                bounds,
                encounter,
                elapsed);
            previousRunDirection = emergencySteeringActive
                ? direction
                : Vector2.Lerp(
                    previousRunDirection,
                    direction,
                    Mathf.Clamp01(Time.deltaTime * 12f)).normalized;
            DriveBody(body,previousRunDirection,movement.MoveSpeed,bounds);

            if (progress.Level != lastLevel || shooter.EquippedCount != lastWeapons)
            {
                lastLevel = progress.Level;
                lastWeapons = shooter.EquippedCount;
                Debug.Log(
                    $"NEON_FULL_RUN_PROGRESS time={elapsed:0.0} " +
                    $"level={lastLevel} weapons={lastWeapons} " +
                    $"hp={health.Current:0}/{health.Max:0}");
            }
            yield return null;
        }

        if (!manager.HasWon)
        {
            body.linearVelocity = Vector2.zero;
            portfolioShotFailed = true;
            Debug.LogError(
                $"NEON_FULL_RUN_FAIL time={elapsed:0.0} dead={health.IsDead} " +
                $"stage={encounter.Stage} level={progress.Level} " +
                $"weapons={shooter.EquippedCount} hp={health.Current:0}/{health.Max:0}");
            yield break;
        }

        Debug.Log(
            $"NEON_FULL_RUN_VICTORY time={elapsed:0.0} level={progress.Level} " +
            $"weapons={shooter.EquippedCount} hp={health.Current:0}/{health.Max:0}");
        body.linearVelocity = Vector2.zero;
        yield return new WaitForSecondsRealtime(7f);
    }

    private IEnumerator RunBossPractice()
    {
        yield return StartRunAndSelectWeapon();
        var manager = FindFirstObjectByType<GameManager>();
        var player = GameObject.FindGameObjectWithTag("Player");
        var body = player.GetComponent<Rigidbody2D>();
        var movement = player.GetComponent<PlayerMovement>();
        var health = player.GetComponent<Health>();
        var shooter = player.GetComponent<PlayerShooter>();
        var bounds = FindFirstObjectByType<ArenaBounds>();
        var encounter = manager.GetComponent<BossEncounter>();
        movement.enabled = false;

        var weapons = Resources.FindObjectsOfTypeAll<WeaponDefinition>()
            .OrderBy(weapon => weapon.WeaponType).ToArray();
        var weaponIndex = 0;
        while (shooter.EquippedCount < shooter.WeaponSlotCapacity)
        {
            shooter.EquipWeapon(weapons[weaponIndex%weapons.Length]);
            weaponIndex++;
        }
        health.TakeDamage(Mathf.Max(0f,health.Current-42f));
        yield return WaitForStage(encounter,BossStage.MiniFight,8f);

        var elapsed = 0f;
        var previousHealth = health.Current;
        while (elapsed < 45f && !health.IsDead &&
               encounter.Stage == BossStage.MiniFight)
        {
            elapsed += Time.unscaledDeltaTime;
            var direction = ChooseFullRunDirection(
                body.position,bounds,encounter,elapsed);
            previousRunDirection = emergencySteeringActive
                ? direction
                : Vector2.Lerp(
                    previousRunDirection,
                    direction,
                    Mathf.Clamp01(Time.deltaTime*12f)).normalized;
            DriveBody(body,previousRunDirection,movement.MoveSpeed,bounds);
            if (health.Current < previousHealth)
            {
                LogBossPracticeHit(
                    elapsed,body.position,health,encounter);
                previousHealth = health.Current;
            }
            yield return null;
        }

        if (health.IsDead || encounter.Stage != BossStage.Rewards)
        {
            portfolioShotFailed = true;
            Debug.LogError(
                $"NEON_BOSS_PRACTICE_FAIL time={elapsed:0.0} " +
                $"stage={encounter.Stage} hp={health.Current:0}/{health.Max:0}");
            yield break;
        }
        Debug.Log(
            $"NEON_BOSS_PRACTICE_MINI_PASS time={elapsed:0.0} " +
            $"hp={health.Current:0}/{health.Max:0}");

        body.linearVelocity=Vector2.zero;
        encounter.ChooseReward(3);
        yield return new WaitForSecondsRealtime(.5f);
        encounter.ChooseReward(0);
        yield return WaitForStage(encounter,BossStage.FinalFight,8f);

        elapsed=0f;
        previousHealth=health.Current;
        while(elapsed<90f && !health.IsDead && !manager.HasWon)
        {
            elapsed+=Time.unscaledDeltaTime;
            var direction=ChooseFullRunDirection(
                body.position,bounds,encounter,elapsed);
            previousRunDirection=emergencySteeringActive
                ? direction
                : Vector2.Lerp(
                    previousRunDirection,
                    direction,
                    Mathf.Clamp01(Time.deltaTime*12f)).normalized;
            DriveBody(body,previousRunDirection,movement.MoveSpeed,bounds);
            if(health.Current<previousHealth)
            {
                LogBossPracticeHit(elapsed,body.position,health,encounter);
                previousHealth=health.Current;
            }
            yield return null;
        }

        body.linearVelocity=Vector2.zero;
        if(health.IsDead || !manager.HasWon)
        {
            portfolioShotFailed=true;
            Debug.LogError(
                $"NEON_BOSS_PRACTICE_FINAL_FAIL time={elapsed:0.0} " +
                $"stage={encounter.Stage} hp={health.Current:0}/{health.Max:0}");
            yield break;
        }
        Debug.Log(
            $"NEON_BOSS_PRACTICE_PASS time={elapsed:0.0} " +
            $"hp={health.Current:0}/{health.Max:0}");
    }

    private static void LogBossPracticeHit(
        float elapsed,
        Vector2 playerPosition,
        Health health,
        BossEncounter encounter)
    {
        var bossDistance = encounter.ActiveBoss == null
            ? -1f
            : Vector2.Distance(
                playerPosition,
                encounter.ActiveBoss.transform.position);
        var hazardDetails = FindObjectsByType<BossHazard>(FindObjectsSortMode.None)
            .Where(hazard => hazard.gameObject.activeInHierarchy)
            .Select(hazard =>
            {
                var zone = HazardZoneField != null &&
                    (bool)HazardZoneField.GetValue(hazard);
                var radius = HazardRadiusField != null
                    ? (float)HazardRadiusField.GetValue(hazard)
                    : 0f;
                var distance = Vector2.Distance(
                    playerPosition,hazard.transform.position);
                return $"{(zone ? "zone" : "shot")}:{distance:0.00}/r{radius:0.00}";
            });
        Debug.Log(
            $"NEON_BOSS_PRACTICE_HIT time={elapsed:0.00} " +
            $"hp={health.Current:0}/{health.Max:0} bossDistance={bossDistance:0.00} " +
            $"hazards=[{string.Join(",",hazardDetails)}]");
    }

    private static int ChooseStartingWeaponIndex()
    {
        // The sorted starting choices are pistol, SMG and laser. The SMG gives
        // reliable early coverage while still following the normal loadout UI.
        return 1;
    }

    private static int ChooseUpgradeIndex(
        UpgradeController upgrades,
        Health health,
        PlayerShooter shooter)
    {
        var choices = CurrentChoicesField?.GetValue(upgrades)
            as PlayerUpgradeData[];
        if (choices == null)
        {
            return 0;
        }

        var bestIndex = 0;
        var bestScore = int.MinValue;
        for (var i = 0; i < choices.Length; i++)
        {
            var choice = choices[i];
            if (choice == null) continue;

            var score = UpgradeScore(choice,health,shooter);
            if (score > bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
        }
        return bestIndex;
    }

    private static int UpgradeScore(
        PlayerUpgradeData choice,
        Health health,
        PlayerShooter shooter)
    {
        if (choice.EffectType == UpgradeEffectType.EquipWeapon &&
            shooter.EquippedCount < shooter.WeaponSlotCapacity)
        {
            var weaponBonus = choice.Weapon != null &&
                choice.Weapon.WeaponType == WeaponType.Laser ? 5 : 0;
            return 1000 + weaponBonus;
        }

        var healthRatio = health.Max > 0f ? health.Current / health.Max : 0f;
        switch (choice.EffectType)
        {
            case UpgradeEffectType.Heal:
                return healthRatio < .5f ? 900 : healthRatio < .75f ? 620 : 80;
            case UpgradeEffectType.MaxHealth:
                return healthRatio < .65f ? 760 : 560;
            case UpgradeEffectType.MoveSpeed:
                return 520;
            case UpgradeEffectType.Damage:
                return 470;
            case UpgradeEffectType.FireRate:
                return 450;
            case UpgradeEffectType.AttackRange:
                return 420;
            case UpgradeEffectType.PickupRange:
                return 360;
            default:
                return 0;
        }
    }

    private Vector2 ChooseFullRunDirection(
        Vector2 position,
        ArenaBounds bounds,
        BossEncounter encounter,
        float elapsed)
    {
        var enemies = EnemyRegistry.ActiveEnemies
            .Where(enemy => enemy != null && enemy.gameObject.activeInHierarchy)
            .ToArray();
        var hazards = FindObjectsByType<BossHazard>(FindObjectsSortMode.None)
            .Where(hazard => hazard.gameObject.activeInHierarchy)
            .ToArray();
        var pickups = FindObjectsByType<ExperiencePickup>(FindObjectsSortMode.None)
            .Where(pickup => pickup.gameObject.activeInHierarchy)
            .ToArray();
        var warnings = FindObjectsByType<EnemySpawnWarning>(FindObjectsSortMode.None)
            .Where(warning => warning.gameObject.activeInHierarchy)
            .ToArray();

        emergencySteeringActive = false;
        var emergency = ChooseEmergencyDirection(
            position,bounds,hazards,encounter);
        if (emergency.sqrMagnitude > .01f)
        {
            emergencySteeringActive = true;
            return emergency.normalized;
        }
        emergencySteeringActive = hazards.Any(hazard =>
        {
            var zone = HazardZoneField != null &&
                (bool)HazardZoneField.GetValue(hazard);
            var radius = HazardRadiusField != null
                ? (float)HazardRadiusField.GetValue(hazard)
                : .3f;
            var velocity = HazardVelocityField != null
                ? (Vector2)HazardVelocityField.GetValue(hazard)
                : Vector2.zero;
            var hazardPosition = (Vector2)hazard.transform.position;
            return zone
                ? Vector2.Distance(position,hazardPosition)-radius < 1.5f
                : PointSegmentDistance(
                    position,
                    hazardPosition,
                    hazardPosition+velocity.normalized*10f)-radius < 1.8f;
        });
        emergencySteeringActive |= enemies.Any(enemy =>
        {
            var preparing = EnemyPreparingDashField != null &&
                (bool)EnemyPreparingDashField.GetValue(enemy);
            var dashing = EnemyDashingField != null &&
                (bool)EnemyDashingField.GetValue(enemy);
            return (preparing || dashing) &&
                Vector2.Distance(position,enemy.transform.position)<8f;
        });

        var bestDirection = previousRunDirection;
        var bestScore = float.NegativeInfinity;
        const int directionCount = 48;
        for (var i = 0; i < directionCount; i++)
        {
            var angle = i * Mathf.PI * 2f / directionCount;
            var direction = new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
            var score = ScoreDirection(
                position,
                direction,
                bounds,
                enemies,
                hazards,
                pickups,
                warnings,
                encounter,
                elapsed);
            if (score > bestScore)
            {
                bestScore = score;
                bestDirection = direction;
            }
        }
        return bestDirection;
    }

    private Vector2 ChooseEmergencyDirection(
        Vector2 position,
        ArenaBounds bounds,
        BossHazard[] hazards,
        BossEncounter encounter)
    {
        var boss = encounter.ActiveBoss;
        if (boss != null)
        {
            var bossPosition = (Vector2)boss.transform.position;
            if (BossDashUntilField != null &&
                BossLockedDirectionField != null &&
                (float)BossDashUntilField.GetValue(boss) > Time.time)
            {
                var lockedDirection =
                    (Vector2)BossLockedDirectionField.GetValue(boss);
                var dashEnd = bossPosition+lockedDirection*(boss.IsFinal?11f:8f);
                if (PointSegmentDistance(position,bossPosition,dashEnd) < 3f)
                {
                    var perpendicular = Vector2.Perpendicular(lockedDirection).normalized;
                    return ChooseBoundarySafeDirection(
                        position,perpendicular,-perpendicular,bounds);
                }
            }

            var awayFromBoss = position-bossPosition;
            if (awayFromBoss.magnitude < (boss.IsFinal ? 4.8f : 3.6f))
            {
                if (awayFromBoss.sqrMagnitude < .04f)
                    awayFromBoss = -previousRunDirection;
                return awayFromBoss.normalized;
            }
        }

        if (boss != null && boss.IsPreparing &&
            BossLockedDirectionField != null && BossAttackIndexField != null)
        {
            var attackIndex = (int)BossAttackIndexField.GetValue(boss);
            if (attackIndex%3 != 1)
            {
                var lockedDirection =
                    (Vector2)BossLockedDirectionField.GetValue(boss);
                var bossPosition = (Vector2)boss.transform.position;
                var lineEnd = bossPosition+lockedDirection*13f;
                if (PointSegmentDistance(position,bossPosition,lineEnd) < 2.2f)
                {
                    var perpendicular = Vector2.Perpendicular(lockedDirection).normalized;
                    return ChooseBoundarySafeDirection(
                        position,perpendicular,-perpendicular,bounds);
                }
            }
        }
        return Vector2.zero;
    }

    private Vector2 ChooseBoundarySafeDirection(
        Vector2 position,
        Vector2 first,
        Vector2 second,
        ArenaBounds bounds)
    {
        var firstPoint = position+first*3f;
        var secondPoint = position+second*3f;
        var firstScore = BoundaryMargin(firstPoint,bounds)+
            Vector2.Dot(first,previousRunDirection)*1.5f;
        var secondScore = BoundaryMargin(secondPoint,bounds)+
            Vector2.Dot(second,previousRunDirection)*1.5f;
        return firstScore >= secondScore ? first : second;
    }

    private static float BoundaryMargin(Vector2 point,ArenaBounds bounds)
    {
        var min = bounds.Minimum+Vector2.one*1.2f;
        var max = bounds.Maximum-Vector2.one*1.2f;
        return Mathf.Min(
            point.x-min.x,
            max.x-point.x,
            point.y-min.y,
            max.y-point.y);
    }

    private float ScoreDirection(
        Vector2 position,
        Vector2 direction,
        ArenaBounds bounds,
        EnemyController[] enemies,
        BossHazard[] hazards,
        ExperiencePickup[] pickups,
        EnemySpawnWarning[] warnings,
        BossEncounter encounter,
        float elapsed)
    {
        var candidate = position + direction * 3.2f;
        var score = Vector2.Dot(direction,previousRunDirection) * 2.5f;

        var min = bounds.Minimum + new Vector2(5f,4f);
        var max = bounds.Maximum - new Vector2(5f,4f);
        var wallMargin = Mathf.Min(
            candidate.x - min.x,
            max.x - candidate.x,
            candidate.y - min.y,
            max.y - candidate.y);
        if (wallMargin < 0f) score += wallMargin * 300f;
        else if (wallMargin < 3f) score -= (3f - wallMargin) * 50f;
        else score += Mathf.Min(wallMargin,5f) * 1.2f;

        var center = (bounds.Minimum+bounds.Maximum)*.5f;
        if (!new Rect(min,max-min).Contains(position))
            score += Vector2.Dot(direction,(center-position).normalized)*80f;

        var nearestEnemy = float.PositiveInfinity;
        foreach (var enemy in enemies)
        {
            var distance = Vector2.Distance(candidate,enemy.transform.position);
            var pathDistance = PointSegmentDistance(
                enemy.transform.position,
                position,
                candidate);
            nearestEnemy = Mathf.Min(nearestEnemy,distance);
            var isBoss = enemy.GetComponent<BossAgent>() != null;
            var bossAgent = isBoss ? enemy.GetComponent<BossAgent>() : null;
            var safeDistance = isBoss && bossAgent.IsFinal ? 5f : isBoss ? 4.4f : 5f;
            if (distance < safeDistance)
            {
                var danger = safeDistance - distance;
                score -= danger * danger * (isBoss ? 60f : 24f);
            }
            if (pathDistance < (isBoss ? 2.8f : 1.6f))
            {
                var danger = (isBoss ? 2.8f : 1.6f) - pathDistance;
                score -= danger * danger * (isBoss ? 100f : 65f);
            }

            if (!isBoss)
            {
                var preparingDash = EnemyPreparingDashField != null &&
                    (bool)EnemyPreparingDashField.GetValue(enemy);
                var dashing = EnemyDashingField != null &&
                    (bool)EnemyDashingField.GetValue(enemy);
                if (preparingDash || dashing)
                {
                    var enemyPosition = (Vector2)enemy.transform.position;
                    var dashDirection = dashing && EnemyDashDirectionField != null
                        ? (Vector2)EnemyDashDirectionField.GetValue(enemy)
                        : (position-enemyPosition).normalized;
                    var dashEnd = enemyPosition+dashDirection*9f;
                    var dashDistance = PointSegmentDistance(
                        candidate,enemyPosition,dashEnd);
                    if (dashDistance < 2.4f)
                    {
                        var danger = 2.4f-dashDistance;
                        score -= danger*danger*180f;
                    }
                }
            }
        }

        foreach (var warning in warnings)
        {
            var distance = Vector2.Distance(candidate,warning.transform.position);
            if (distance < 3.2f)
            {
                var danger = 3.2f-distance;
                score -= danger*danger*25f;
            }
        }

        foreach (var hazard in hazards)
        {
            var zone = HazardZoneField != null &&
                (bool)HazardZoneField.GetValue(hazard);
            var radius = HazardRadiusField != null
                ? (float)HazardRadiusField.GetValue(hazard)
                : .3f;
            var velocity = HazardVelocityField != null
                ? (Vector2)HazardVelocityField.GetValue(hazard)
                : Vector2.zero;
            var hazardPosition = (Vector2)hazard.transform.position;
            var predictedPosition = zone
                ? hazardPosition
                : hazardPosition+velocity*.9f;
            var clearance = zone
                ? Vector2.Distance(candidate,hazardPosition)-radius
                : PointSegmentDistance(
                    candidate,
                    hazardPosition,
                    predictedPosition)-radius;
            var pathClearance = zone
                ? PointSegmentDistance(hazardPosition,position,candidate)-radius
                : Mathf.Min(
                    PointSegmentDistance(position,hazardPosition,predictedPosition),
                    PointSegmentDistance(candidate,hazardPosition,predictedPosition))-radius;
            var safeClearance = zone ? 2.2f : 1.5f;
            if (clearance < safeClearance)
            {
                var danger = safeClearance - clearance;
                score -= danger * danger * (zone ? 120f : 85f);
            }
            if (pathClearance < safeClearance)
            {
                var danger = safeClearance-pathClearance;
                score -= danger*danger*(zone ? 160f : 120f);
            }
        }

        if (encounter.ActiveBoss != null && encounter.ActiveBoss.IsPreparing)
        {
            var bossPosition = (Vector2)encounter.ActiveBoss.transform.position;
            var radial = (candidate - bossPosition).normalized;
            score += Mathf.Abs(Vector2.Dot(direction,Vector2.Perpendicular(radial))) * 12f;

            var attackIndex = BossAttackIndexField != null
                ? (int)BossAttackIndexField.GetValue(encounter.ActiveBoss)
                : 1;
            if (attackIndex%3 != 1 && BossLockedDirectionField != null)
            {
                var lockedDirection =
                    (Vector2)BossLockedDirectionField.GetValue(encounter.ActiveBoss);
                var lineEnd = bossPosition+lockedDirection*13f;
                var lineDistance = PointSegmentDistance(candidate,bossPosition,lineEnd);
                var movementLineDistance = Mathf.Min(
                    PointSegmentDistance(position,bossPosition,lineEnd),
                    lineDistance);
                if (lineDistance < 2.1f)
                    score -= (2.1f-lineDistance)*(2.1f-lineDistance)*180f;
                if (movementLineDistance < 1.2f)
                    score -= (1.2f-movementLineDistance)*
                        (1.2f-movementLineDistance)*220f;
            }
        }

        if (encounter.ActiveBoss != null)
        {
            var toBoss = (Vector2)encounter.ActiveBoss.transform.position-position;
            if (toBoss.magnitude > 5.5f)
                score += Vector2.Dot(direction,toBoss.normalized)*
                    Mathf.Min(30f,(toBoss.magnitude-5.5f)*10f);
        }

        if (encounter.Stage == BossStage.Collecting &&
            nearestEnemy > 4.5f && pickups.Length > 0)
        {
            ExperiencePickup closest = null;
            var closestDistance = float.PositiveInfinity;
            foreach (var pickup in pickups)
            {
                var distance = Vector2.Distance(position,pickup.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = pickup;
                }
            }
            if (closest != null)
            {
                var toPickup = ((Vector2)closest.transform.position-position).normalized;
                score += Vector2.Dot(direction,toPickup) *
                    Mathf.Lerp(13f,4f,Mathf.Clamp01(closestDistance/10f));
            }
        }

        // A small changing bias avoids settling into one mechanical orbit while
        // remaining far weaker than the real danger and boundary scores.
        var flow = new Vector2(
            Mathf.Cos(elapsed*.37f+position.y*.11f),
            Mathf.Sin(elapsed*.29f-position.x*.09f));
        score += Vector2.Dot(direction,flow.normalized) * 1.8f;
        return score;
    }

    private static float PointSegmentDistance(
        Vector2 point,
        Vector2 from,
        Vector2 to)
    {
        var segment = to-from;
        if (segment.sqrMagnitude < .0001f)
            return Vector2.Distance(point,from);
        var t = Mathf.Clamp01(Vector2.Dot(point-from,segment)/segment.sqrMagnitude);
        return Vector2.Distance(point,from+segment*t);
    }

    private static void DriveBody(
        Rigidbody2D body,
        Vector2 direction,
        float speed,
        ArenaBounds bounds)
    {
        var position = bounds.ClampPoint(body.position,.55f);
        if ((position-body.position).sqrMagnitude > .0001f)
            body.position = position;

        var velocity = direction.normalized*speed;
        var minimum = bounds.Minimum+Vector2.one*.55f;
        var maximum = bounds.Maximum-Vector2.one*.55f;
        if ((position.x <= minimum.x+.02f && velocity.x < 0f) ||
            (position.x >= maximum.x-.02f && velocity.x > 0f))
            velocity.x = 0f;
        if ((position.y <= minimum.y+.02f && velocity.y < 0f) ||
            (position.y >= maximum.y-.02f && velocity.y > 0f))
            velocity.y = 0f;
        body.linearVelocity = velocity;
    }

    private static void InvokeUpgradeButton(int index)
    {
        var button = GameObject.Find($"UpgradeButton{index+1}")
            ?.GetComponent<Button>();
        if (button == null)
        {
            throw new InvalidOperationException(
                $"Upgrade button {index+1} was not found.");
        }
        button.onClick.Invoke();
    }

    private IEnumerator RunCombatShot(float duration,bool advanced)
    {
        yield return StartRunAndSelectWeapon();
        var player = GameObject.FindGameObjectWithTag("Player");
        var movement = player.GetComponent<PlayerMovement>();
        movement.enabled = false;
        var spawner = FindFirstObjectByType<EnemySpawner>();
        spawner.enabled = false;
        var entries = GetEnemyEntries(spawner).OrderBy(value => value.UnlockTime).ToArray();
        var enemyPool = GameObject.Find("EnemyPool").GetComponent<GameObjectPool>();
        var experiencePool = GameObject.Find("ExperiencePool").GetComponent<GameObjectPool>();
        var body = player.GetComponent<Rigidbody2D>();
        var start = body.position;
        var upgrades = FindFirstObjectByType<UpgradeController>();
        var elapsed = 0f;
        var nextSpawn = 0f;
        var choiceElapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (upgrades.IsOpen)
            {
                choiceElapsed += Time.unscaledDeltaTime;
                if (choiceElapsed >= 2.2f)
                {
                    GameObject.Find("UpgradeButton1").GetComponent<Button>().onClick.Invoke();
                    choiceElapsed = 0f;
                }
                yield return null;
                continue;
            }
            choiceElapsed = 0f;
            var target = start + new Vector2(
                Mathf.Cos(elapsed * 0.82f) * 4.2f,
                Mathf.Sin(elapsed * 0.82f) * 3.1f);
            body.position = Vector2.Lerp(body.position,target,Time.deltaTime * 3.4f);

            if (elapsed >= nextSpawn)
            {
                nextSpawn += 1.25f;
                var angle = elapsed * 1.7f;
                var offset = new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0f) * 5.1f;
                var enemy = enemyPool.Get(player.transform.position + offset,Quaternion.identity)
                    .GetComponent<EnemyController>();
                var definition = advanced
                    ? entries[Mathf.FloorToInt(elapsed) % entries.Length].Definition
                    : entries[0].Definition;
                enemy.Spawn(player.transform,experiencePool,definition);
            }
            yield return null;
        }
    }

    private IEnumerator RunAuthenticAdvancedShot(float duration)
    {
        yield return StartRunAndSelectWeapon();
        var player = GameObject.FindGameObjectWithTag("Player");
        player.GetComponent<PlayerMovement>().enabled = false;
        var body = player.GetComponent<Rigidbody2D>();
        var movement = player.GetComponent<PlayerMovement>();
        var bounds = FindFirstObjectByType<ArenaBounds>();
        var upgrades = FindFirstObjectByType<UpgradeController>();
        var elapsed = 0f;
        var choiceElapsed = 0f;

        while (elapsed < duration && !player.GetComponent<Health>().IsDead)
        {
            elapsed += Time.unscaledDeltaTime;
            if (upgrades.IsOpen)
            {
                choiceElapsed += Time.unscaledDeltaTime;
                if (choiceElapsed >= 1.4f)
                {
                    GameObject.Find("UpgradeButton1").GetComponent<Button>().onClick.Invoke();
                    choiceElapsed = 0f;
                }
                yield return null;
                continue;
            }

            choiceElapsed = 0f;
            var direction = ChooseSurvivalDirection(body.position,bounds,elapsed);
            body.MovePosition(bounds.ClampPoint(
                body.position + direction * movement.MoveSpeed * Time.deltaTime,.55f));
            yield return null;
        }

        var progress = player.GetComponent<PlayerProgress>();
        var shooter = player.GetComponent<PlayerShooter>();
        if (player.GetComponent<Health>().IsDead || progress.Level < 3 || shooter.EquippedCount < 2)
            throw new InvalidOperationException(
                $"Authentic advanced run failed: time={elapsed:0.0} level={progress.Level} weapons={shooter.EquippedCount}");
    }

    private static Vector2 ChooseSurvivalDirection(Vector2 position,ArenaBounds bounds,float elapsed)
    {
        var steering = Vector2.zero;
        EnemyController nearest = null;
        var nearestDistance = float.PositiveInfinity;
        foreach (var enemy in EnemyRegistry.ActiveEnemies)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;
            var away = position - (Vector2)enemy.transform.position;
            var distance = Mathf.Max(.2f,away.magnitude);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = enemy;
            }
            if (distance < 8f)
                steering += away.normalized * Mathf.Pow(8f - distance,2f);
        }

        if (nearest != null)
        {
            var away = (position - (Vector2)nearest.transform.position).normalized;
            var tangent = new Vector2(-away.y,away.x);
            if (Mathf.FloorToInt(elapsed / 8f) % 2 == 1) tangent = -tangent;
            steering += tangent * 7f;
        }

        if (nearestDistance > 4.5f)
        {
            ExperiencePickup closest = null;
            var pickupDistance = 7f;
            foreach (var pickup in FindObjectsByType<ExperiencePickup>(FindObjectsSortMode.None))
            {
                if (!pickup.gameObject.activeInHierarchy) continue;
                var distance = Vector2.Distance(position,pickup.transform.position);
                if (distance < pickupDistance)
                {
                    pickupDistance = distance;
                    closest = pickup;
                }
            }
            if (closest != null)
                steering += ((Vector2)closest.transform.position - position).normalized * 6f;
        }

        var min = bounds.Minimum + Vector2.one * 3f;
        var max = bounds.Maximum - Vector2.one * 3f;
        if (position.x < min.x) steering.x += (min.x - position.x) * 8f;
        if (position.x > max.x) steering.x -= (position.x - max.x) * 8f;
        if (position.y < min.y) steering.y += (min.y - position.y) * 8f;
        if (position.y > max.y) steering.y -= (position.y - max.y) * 8f;
        if (steering.sqrMagnitude < .01f)
            steering = new Vector2(Mathf.Cos(elapsed),Mathf.Sin(elapsed));
        return steering.normalized;
    }

    private IEnumerator RunUpgradeShot()
    {
        yield return StartRunAndSelectWeapon();
        var player = GameObject.FindGameObjectWithTag("Player");
        var spawner = FindFirstObjectByType<EnemySpawner>();
        spawner.enabled = false;
        var entries = GetEnemyEntries(spawner);
        var enemyPool = GameObject.Find("EnemyPool").GetComponent<GameObjectPool>();
        var experiencePool = GameObject.Find("ExperiencePool").GetComponent<GameObjectPool>();

        for (var i = 0; i < 5; i++)
        {
            var angle = i * Mathf.PI * 2f / 5f;
            var position = player.transform.position +
                new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0f) * 4.5f;
            enemyPool.Get(position,Quaternion.identity).GetComponent<EnemyController>()
                .Spawn(player.transform,experiencePool,entries[i % entries.Length].Definition);
        }

        yield return new WaitForSecondsRealtime(4f);
        var progress = player.GetComponent<PlayerProgress>();
        progress.AddExperience(progress.ExperienceToNextLevel - progress.Experience);
        yield return new WaitForSecondsRealtime(7f);
        GameObject.Find("UpgradeButton1").GetComponent<Button>().onClick.Invoke();
        yield return new WaitForSecondsRealtime(4f);
    }

    private IEnumerator RunBossShot(bool final)
    {
        yield return StartRunAndSelectWeapon();
        var manager = FindFirstObjectByType<GameManager>();
        var player = GameObject.FindGameObjectWithTag("Player");
        var movement = player.GetComponent<PlayerMovement>();
        movement.enabled = false;
        FindFirstObjectByType<EnemySpawner>().enabled = false;
        var shooter = player.GetComponent<PlayerShooter>();
        var definition = Resources.FindObjectsOfTypeAll<WeaponDefinition>()
            .OrderBy(value => value.WeaponType).First();
        while (shooter.EquippedCount < shooter.WeaponSlotCapacity)
            shooter.EquipWeapon(definition);

        var encounter = manager.GetComponent<BossEncounter>();
        yield return WaitForStage(encounter,BossStage.MiniFight,8f);
        if (!final)
        {
            yield return MoveAroundBoss(player,encounter,14f);
            yield break;
        }

        yield return MoveAroundBoss(player,encounter,5f);
        encounter.ActiveBoss.Health.TakeDamage(1000000f);
        yield return WaitForStage(encounter,BossStage.Rewards,4f);
        yield return new WaitForSecondsRealtime(5f);
        encounter.ChooseReward(0);
        yield return new WaitForSecondsRealtime(1.2f);
        encounter.ChooseReward(2);
        yield return WaitForStage(encounter,BossStage.FinalFight,8f);
        yield return MoveAroundBoss(player,encounter,9f);
        encounter.ActiveBoss.Health.TakeDamage(1000000f);
        yield return new WaitForSecondsRealtime(6f);
    }

    private static IEnumerator StartRunAndSelectWeapon()
    {
        FindFirstObjectByType<GameManager>().StartRun();
        yield return null;
        yield return null;
        GameObject.Find("UpgradeButton1").GetComponent<Button>().onClick.Invoke();
        yield return null;
    }

    private static IEnumerator WaitForStage(BossEncounter encounter,BossStage stage,float timeout)
    {
        var elapsed = 0f;
        while (encounter.Stage != stage && elapsed < timeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private static IEnumerator MoveAroundBoss(GameObject player,BossEncounter encounter,float duration)
    {
        var body = player.GetComponent<Rigidbody2D>();
        var center = body.position;
        var elapsed = 0f;
        while (elapsed < duration && encounter.ActiveBoss != null)
        {
            elapsed += Time.unscaledDeltaTime;
            var target = center + new Vector2(
                Mathf.Cos(elapsed * 1.35f) * 4.8f,
                Mathf.Sin(elapsed * 1.75f) * 3.4f);
            body.position = Vector2.Lerp(body.position,target,Time.deltaTime * 3.8f);
            yield return null;
        }
    }

    private static EnemySpawnEntry[] GetEnemyEntries(EnemySpawner spawner)
    {
        return (EnemySpawnEntry[])typeof(EnemySpawner)
            .GetField("enemyTypes",BindingFlags.Instance | BindingFlags.NonPublic)
            .GetValue(spawner);
    }

    private static string ReadArgument(string[] args,string name)
    {
        var index = Array.IndexOf(args,name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : string.Empty;
    }
}
