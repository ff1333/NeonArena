using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ReleasePipeline
{
    public const string Version = "1.4.0";
    private const string ReleaseBranch = "feature/intelligent-combat-ai";
    private const string MainScene = "Assets/Scenes/Main.unity";
    private const string Identifier = "com.ff1333.neonarenarebuild";
    private const string VerificationReport =
        "docs/devlogs/22-v1.4.0-build-verification.md";
    private const string BossConfigPath =
        "Assets/Resources/Combat/BossDecisionConfig.asset";
    private const string DirectorConfigPath =
        "Assets/Resources/Combat/CombatDirectorConfig.asset";
    private const string ManualResultsHeading = "## Platform Builds";

    [MenuItem("Build/Neon Arena/Configure Release Settings")]
    public static void ConfigureReleaseSettings()
    {
        PlayerSettings.companyName = "FF1333";
        PlayerSettings.productName = "Neon Arena Rebuild";
        PlayerSettings.bundleVersion = Version;
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.defaultWebScreenWidth = 960;
        PlayerSettings.defaultWebScreenHeight = 540;
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.defaultIsNativeResolution = false;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;

        PlayerSettings.SetApplicationIdentifier(
            NamedBuildTarget.Standalone,
            Identifier);
        PlayerSettings.SetApplicationIdentifier(
            NamedBuildTarget.Android,
            Identifier);

        PlayerSettings.Android.bundleVersionCode = 5;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel36;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;

        EnsureMainSceneIsFirst();
        AssetDatabase.SaveAssets();
        Debug.Log("Release player settings configured.");
    }

    [MenuItem("Build/Neon Arena/Run Release Boundary Verification")]
    public static void RunBoundaryVerification()
    {
        ConfigureReleaseSettings();

        List<string> passed = new List<string>();
        List<string> failed = new List<string>();

        Verify("Main scene is enabled at build index 0",
            IsMainSceneFirst(), passed, failed);
        Verify("Windows build support is installed",
            BuildPipeline.IsBuildTargetSupported(
                BuildTargetGroup.Standalone,
                BuildTarget.StandaloneWindows64), passed, failed);
        Verify("WebGL build support is installed",
            BuildPipeline.IsBuildTargetSupported(
                BuildTargetGroup.WebGL,
                BuildTarget.WebGL), passed, failed);
        Verify("Android build support is installed",
            BuildPipeline.IsBuildTargetSupported(
                BuildTargetGroup.Android,
                BuildTarget.Android), passed, failed);

        VerifySceneConfiguration(passed, failed);
        VerifyWeaponLoadoutRule(passed, failed);
        VerifyExperiencePickupStyles(passed, failed);
        VerifyPoolExpansionAndDuplicateRelease(passed, failed);
        VerifyCombatAIConfiguration(passed, failed);
        ResetEnemyRegistry();
        Verify("Target selection returns null with no enemies",
            TargetSelector.FindPriorityTarget(Vector2.zero, 20f, 1.5f) == null,
            passed, failed);

        Verify("Company name is release-ready",
            PlayerSettings.companyName == "FF1333", passed, failed);
        Verify("Product name is release-ready",
            PlayerSettings.productName == "Neon Arena Rebuild", passed, failed);
        Verify($"Version is {Version}",
            PlayerSettings.bundleVersion == Version, passed, failed);
        Verify("Android version code is 5",
            PlayerSettings.Android.bundleVersionCode == 5, passed, failed);
        Verify("Android identifier is release-ready",
            PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) ==
            Identifier, passed, failed);
        Verify("Portrait autorotation is disabled",
            !PlayerSettings.allowedAutorotateToPortrait &&
            !PlayerSettings.allowedAutorotateToPortraitUpsideDown,
            passed, failed);
        Verify("Both landscape orientations are enabled",
            PlayerSettings.allowedAutorotateToLandscapeLeft &&
            PlayerSettings.allowedAutorotateToLandscapeRight,
            passed, failed);

        WriteVerificationReport(passed, failed);
        AssetDatabase.Refresh();

        if (failed.Count > 0)
        {
            Debug.LogError(
                $"Release boundary verification failed: {failed.Count} checks.");
        }
        else
        {
            Debug.Log(
                $"Release boundary verification passed: {passed.Count} checks.");
        }

        ExitBatchMode(failed.Count == 0);
    }

    [MenuItem("Build/Neon Arena/Build Windows " + Version)]
    public static void BuildWindows()
    {
        ConfigureReleaseSettings();
        Build(
            BuildTarget.StandaloneWindows64,
            $"Builds/Windows/v{Version}/NeonArenaRebuild.exe");
    }

    [MenuItem("Build/Neon Arena/Build WebGL " + Version)]
    public static void BuildWebGL()
    {
        ConfigureReleaseSettings();
        Build(BuildTarget.WebGL, $"Builds/WebGL/v{Version}");
    }

    [MenuItem("Build/Neon Arena/Build Android APK " + Version)]
    public static void BuildAndroid()
    {
        ConfigureJavaProxyFromSystem();
        ConfigureReleaseSettings();
        EditorUserBuildSettings.buildAppBundle = false;
        Build(
            BuildTarget.Android,
            $"Builds/Android/v{Version}/NeonArenaRebuild-v{Version}.apk");
    }

    private static void ConfigureJavaProxyFromSystem()
    {
#if UNITY_EDITOR_WIN
        try
        {
            Uri destination = new Uri("https://dl.google.com");
            IWebProxy proxy = WebRequest.DefaultWebProxy;
            Uri proxyUri = proxy?.GetProxy(destination);
            if (proxyUri == null || proxyUri == destination)
            {
                return;
            }

            string options = Environment.GetEnvironmentVariable(
                "JAVA_TOOL_OPTIONS") ?? string.Empty;
            if (options.Contains("-Dhttps.proxyHost="))
            {
                return;
            }

            string proxyOptions =
                $"-Djava.net.preferIPv4Stack=true " +
                $"-Dhttp.proxyHost={proxyUri.Host} " +
                $"-Dhttp.proxyPort={proxyUri.Port} " +
                $"-Dhttps.proxyHost={proxyUri.Host} " +
                $"-Dhttps.proxyPort={proxyUri.Port} " +
                "-Dhttp.nonProxyHosts=localhost|127.*";
            Environment.SetEnvironmentVariable(
                "JAVA_TOOL_OPTIONS",
                string.IsNullOrWhiteSpace(options)
                    ? proxyOptions
                    : options + " " + proxyOptions);
            Debug.Log(
                $"Configured Android Java proxy from Windows settings: " +
                $"{proxyUri.Host}:{proxyUri.Port}");
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "Could not configure the Android Java proxy automatically: " +
                exception.Message);
        }
#endif
    }

    private static void VerifySceneConfiguration(
        List<string> passed,
        List<string> failed)
    {
        EditorSceneManager.OpenScene(MainScene);

        GameObject[] objects = UnityEngine.Object.FindObjectsByType<GameObject>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        int missingScripts = 0;
        for (int i = 0; i < objects.Length; i++)
        {
            missingScripts +=
                GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(objects[i]);
        }
        Verify("Main scene contains no missing scripts",
            missingScripts == 0, passed, failed);

        GameObjectPool[] pools =
            UnityEngine.Object.FindObjectsByType<GameObjectPool>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
        Verify("Main scene contains four object pools",
            pools.Length == 4, passed, failed);

        bool poolsConfigured = pools.Length == 4;
        for (int i = 0; i < pools.Length; i++)
        {
            SerializedObject serializedPool = new SerializedObject(pools[i]);
            poolsConfigured &=
                serializedPool.FindProperty("prefab").objectReferenceValue != null;
            poolsConfigured &=
                serializedPool.FindProperty("initialSize").intValue > 0;
        }
        Verify("Every object pool has a prefab and positive prewarm size",
            poolsConfigured, passed, failed);

        UpgradeController controller =
            UnityEngine.Object.FindFirstObjectByType<UpgradeController>(
                FindObjectsInactive.Include);
        bool upgradeUiConfigured = controller != null;
        if (controller != null)
        {
            SerializedObject serializedController =
                new SerializedObject(controller);
            upgradeUiConfigured &=
                serializedController.FindProperty("buttons").arraySize == 3;
            upgradeUiConfigured &=
                serializedController.FindProperty("labels").arraySize == 3;
            upgradeUiConfigured &=
                serializedController.FindProperty("weaponIcons").arraySize == 3;
            upgradeUiConfigured &=
                serializedController.FindProperty("availableUpgrades").arraySize == 10;
        }
        Verify("Upgrade UI has three cards and ten release upgrades",
            upgradeUiConfigured, passed, failed);
    }

    private static void VerifyPoolExpansionAndDuplicateRelease(
        List<string> passed,
        List<string> failed)
    {
        GameObject prefab = new GameObject("ReleasePoolTestPrefab");
        prefab.SetActive(false);
        GameObject poolObject = new GameObject("ReleasePoolTest");
        GameObjectPool pool = poolObject.AddComponent<GameObjectPool>();

        try
        {
            SerializedObject serializedPool = new SerializedObject(pool);
            serializedPool.FindProperty("prefab").objectReferenceValue = prefab;
            serializedPool.FindProperty("initialSize").intValue = 1;
            serializedPool.ApplyModifiedPropertiesWithoutUndo();

            InvokePrivate(pool, "Awake");
            GameObject first = pool.Get(Vector3.zero, Quaternion.identity);
            GameObject second = pool.Get(Vector3.right, Quaternion.identity);

            Verify("Pool expands when its prewarmed queue is empty",
                first != null && second != null && first != second,
                passed, failed);

            PoolMember member = first.GetComponent<PoolMember>();
            member.Release();
            member.Release();

            GameObject recycled = pool.Get(Vector3.zero, Quaternion.identity);
            GameObject afterDuplicateRelease =
                pool.Get(Vector3.left, Quaternion.identity);
            Verify("Duplicate Release does not enqueue one instance twice",
                recycled == first && afterDuplicateRelease != first,
                passed, failed);
        }
        catch (Exception exception)
        {
            failed.Add("Pool boundary verification threw: " + exception.Message);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(poolObject);
            UnityEngine.Object.DestroyImmediate(prefab);
        }
    }

    private static void VerifyWeaponLoadoutRule(
        List<string> passed,
        List<string> failed)
    {
        PlayerShooter sceneShooter =
            UnityEngine.Object.FindFirstObjectByType<PlayerShooter>(
                FindObjectsInactive.Include);

        if (sceneShooter == null)
        {
            failed.Add("Main scene contains PlayerShooter");
            return;
        }

        UpgradeController controller =
            UnityEngine.Object.FindFirstObjectByType<UpgradeController>(
                FindObjectsInactive.Include);
        SerializedObject serializedSceneShooter =
            new SerializedObject(sceneShooter);
        WeaponDefinition weapon = null;
        int startingWeaponChoiceCount = 0;
        HashSet<WeaponType> startingWeaponTypes =
            new HashSet<WeaponType>();

        if (controller != null)
        {
            SerializedObject serializedController =
                new SerializedObject(controller);
            SerializedProperty upgrades =
                serializedController.FindProperty("availableUpgrades");

            for (int i = 0; i < upgrades.arraySize; i++)
            {
                PlayerUpgradeData upgrade = upgrades
                    .GetArrayElementAtIndex(i)
                    .objectReferenceValue as PlayerUpgradeData;
                if (upgrade == null ||
                    upgrade.EffectType != UpgradeEffectType.EquipWeapon ||
                    upgrade.Weapon == null)
                {
                    continue;
                }

                weapon ??= upgrade.Weapon;
                startingWeaponChoiceCount++;
                startingWeaponTypes.Add(upgrade.Weapon.WeaponType);
            }
        }

        Verify("Player starts with no pre-equipped weapon",
            serializedSceneShooter.FindProperty("startingWeapon") == null &&
            sceneShooter.EquippedCount == 0, passed, failed);
        Verify("Starting selection has three distinct weapon choices",
            startingWeaponChoiceCount == 3 &&
            startingWeaponTypes.Count == 3, passed, failed);
        GameObject testObject = new GameObject("WeaponLoadoutRuleTest");
        testObject.SetActive(false);

        try
        {
            PlayerShooter testShooter =
                testObject.AddComponent<PlayerShooter>();
            SerializedObject serializedTestShooter =
                new SerializedObject(testShooter);
            SerializedProperty slots =
                serializedTestShooter.FindProperty("weaponSlots");
            slots.arraySize = 6;

            for (int i = 0; i < slots.arraySize; i++)
            {
                GameObject slot = new GameObject($"TestSlot{i}");
                slot.transform.SetParent(testObject.transform);
                slot.AddComponent<SpriteRenderer>();
                slots.GetArrayElementAtIndex(i).objectReferenceValue =
                    slot.transform;
            }

            serializedTestShooter.ApplyModifiedPropertiesWithoutUndo();

            bool filledWithOneType = weapon != null;
            while (filledWithOneType &&
                   testShooter.EquippedCount <
                   testShooter.WeaponSlotCapacity)
            {
                filledWithOneType = testShooter.CanEquip(weapon) &&
                                    testShooter.EquipWeapon(weapon);
            }

            filledWithOneType &= testShooter.WeaponSlotCapacity == 6;
            filledWithOneType &= testShooter.EquippedCount == 6;
            filledWithOneType &= weapon != null &&
                testShooter.GetEquippedCount(weapon.WeaponType) == 6;

            Verify("One weapon type can fill all six weapon slots",
                filledWithOneType, passed, failed);
            Verify("A seventh weapon is rejected by the total slot limit",
                weapon != null && !testShooter.CanEquip(weapon),
                passed, failed);
        }
        catch (Exception exception)
        {
            failed.Add(
                "Weapon loadout verification threw: " + exception.Message);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(testObject);
        }
    }

    private static void VerifyCombatAIConfiguration(
        List<string> passed,
        List<string> failed)
    {
        BossDecisionConfig bossConfig =
            AssetDatabase.LoadAssetAtPath<BossDecisionConfig>(BossConfigPath);
        Verify("Boss decision config exists", bossConfig != null, passed, failed);

        bool bossConfigured = bossConfig != null &&
                              bossConfig.Behaviors != null &&
                              bossConfig.Behaviors.Length == 6;
        if (bossConfigured)
        {
            for (int i = 0; i < bossConfig.Behaviors.Length; i++)
            {
                BossBehaviorTuning tuning = bossConfig.Behaviors[i];
                bossConfigured &= tuning != null &&
                                  tuning.Enabled &&
                                  tuning.Cooldown >= 0f &&
                                  tuning.Windup > 0f &&
                                  tuning.MaximumRange >= tuning.MinimumRange;
            }
        }
        Verify("Boss decision config defines six valid behaviors",
            bossConfigured, passed, failed);

        if (bossConfig != null)
        {
            BossUtilityBrain brain = new BossUtilityBrain(bossConfig);
            BossDecision decision = brain.Evaluate(new BossSituation(
                Vector2.zero,
                Vector2.right * 8f,
                Vector2.right * 4f,
                0.45f,
                0.7f,
                0.2f,
                5f,
                true));
            Verify("Boss utility brain can score all candidates",
                decision.Scores.Count == 6 && decision.HasAction,
                passed, failed);
        }
        else
        {
            failed.Add("Boss utility brain can score all candidates");
        }

        CombatDirectorConfig directorConfig =
            AssetDatabase.LoadAssetAtPath<CombatDirectorConfig>(
                DirectorConfigPath);
        Verify("Combat director config exists", directorConfig != null,
            passed, failed);

        if (directorConfig != null)
        {
            CombatDirectorModel model =
                new CombatDirectorModel(directorConfig);
            CombatDirectorOutput output = model.Output;
            bool bounded = output.SpawnInterval >=
                               directorConfig.FastestSpawnInterval &&
                           output.SpawnInterval <=
                               directorConfig.SlowestSpawnInterval &&
                           output.EnemyCap >= directorConfig.MinimumEnemyCap &&
                           output.EnemyCap <= directorConfig.MaximumEnemyCap &&
                           output.BatchCount >= 0 &&
                           output.BatchCount <= directorConfig.MaximumBatch;

            for (int i = 0; i < 12; i++)
            {
                output = model.Step(new CombatDirectorInput(
                    i * 0.5f,
                    0.5f,
                    1f,
                    0f,
                    2f,
                    1f,
                    i,
                    output.EnemyCap));
                bounded &= output.SpawnInterval >=
                               directorConfig.FastestSpawnInterval &&
                           output.SpawnInterval <=
                               directorConfig.SlowestSpawnInterval &&
                           output.EnemyCap >= directorConfig.MinimumEnemyCap &&
                           output.EnemyCap <= directorConfig.MaximumEnemyCap &&
                           output.BatchCount >= 0 &&
                           output.BatchCount <= directorConfig.MaximumBatch;
            }

            Verify("Combat director output stays inside configured bounds",
                bounded, passed, failed);
        }
        else
        {
            failed.Add("Combat director output stays inside configured bounds");
        }

        Verify("Spawn batch clamps at enemy capacity",
            CombatDirector.ClampBatchToCapacity(0, 10, 3) == 3 &&
            CombatDirector.ClampBatchToCapacity(8, 10, 3) == 2 &&
            CombatDirector.ClampBatchToCapacity(10, 10, 3) == 0,
            passed, failed);
    }

    private static void ResetEnemyRegistry()
    {
        MethodInfo reset = typeof(EnemyRegistry).GetMethod(
            "ResetRegistry",
            BindingFlags.Static | BindingFlags.NonPublic);
        reset?.Invoke(null, null);
    }

    private static void VerifyExperiencePickupStyles(
        List<string> passed,
        List<string> failed)
    {
        string[] definitionPaths =
        {
            "Assets/Data/Enemies/Enemy_Chaser.asset",
            "Assets/Data/Enemies/Enemy_Dasher.asset",
            "Assets/Data/Enemies/Enemy_Berserker.asset"
        };
        HashSet<ExperiencePickupShape> shapes =
            new HashSet<ExperiencePickupShape>();
        HashSet<Color32> colors = new HashSet<Color32>();
        HashSet<int> sprites = new HashSet<int>();
        GameObject testObject = new GameObject("ExperiencePickupStyleTest");
        testObject.SetActive(false);

        try
        {
            ExperiencePickup pickup =
                testObject.AddComponent<ExperiencePickup>();
            InvokePrivate(pickup, "Awake");
            SpriteRenderer renderer =
                testObject.GetComponent<SpriteRenderer>();
            bool stylesApplied = renderer != null;

            for (int i = 0; i < definitionPaths.Length; i++)
            {
                EnemyDefinition definition =
                    AssetDatabase.LoadAssetAtPath<EnemyDefinition>(
                        definitionPaths[i]);
                if (definition == null)
                {
                    stylesApplied = false;
                    continue;
                }

                shapes.Add(definition.ExperiencePickupShape);
                colors.Add((Color32)definition.ExperiencePickupColor);
                pickup.Configure(
                    definition.ExperienceReward,
                    definition.ExperiencePickupShape,
                    definition.ExperiencePickupColor,
                    definition.ExperiencePickupScale);

                stylesApplied &= renderer.sprite != null;
                stylesApplied &= renderer.color ==
                    definition.ExperiencePickupColor;
                stylesApplied &= Mathf.Approximately(
                    testObject.transform.localScale.x,
                    definition.ExperiencePickupScale);

                if (renderer.sprite != null)
                {
                    sprites.Add(renderer.sprite.GetInstanceID());
                }
            }

            Verify("Enemy types use three distinct experience pickup styles",
                shapes.Count == 3 && colors.Count == 3,
                passed,
                failed);
            Verify("Pooled experience pickup reapplies shape color and scale",
                stylesApplied && sprites.Count == 3,
                passed,
                failed);
        }
        catch (Exception exception)
        {
            failed.Add(
                "Experience pickup style verification threw: " +
                exception.Message);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(testObject);
        }
    }

    private static void InvokePrivate(object target, string methodName)
    {
        MethodInfo method = target.GetType().GetMethod(
            methodName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        method?.Invoke(target, null);
    }

    private static void Verify(
        string description,
        bool condition,
        List<string> passed,
        List<string> failed)
    {
        if (condition)
        {
            passed.Add(description);
        }
        else
        {
            failed.Add(description);
        }
    }

    private static void WriteVerificationReport(
        List<string> passed,
        List<string> failed)
    {
        string manualResults = ReadExistingManualResults();
        StringBuilder report = new StringBuilder();
        report.AppendLine("# Multiplatform Release Verification");
        report.AppendLine();
        report.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd}");
        report.AppendLine();
        report.AppendLine($"Branch: `{ReleaseBranch}`");
        report.AppendLine();
        report.AppendLine("## Automated Boundary Verification");
        report.AppendLine();
        report.AppendLine($"Result: {(failed.Count == 0 ? "PASS" : "FAIL")}");
        report.AppendLine();
        for (int i = 0; i < passed.Count; i++)
        {
            report.AppendLine($"- [x] {passed[i]}");
        }
        for (int i = 0; i < failed.Count; i++)
        {
            report.AppendLine($"- [ ] {failed[i]}");
        }
        report.AppendLine();
        report.AppendLine("This verification was executed by an Editor automation tool. " +
            "It is not represented as a manual user test.");
        report.AppendLine();
        report.AppendLine(manualResults);

        Directory.CreateDirectory(Path.GetDirectoryName(VerificationReport));
        File.WriteAllText(VerificationReport, report.ToString());
    }

    private static string ReadExistingManualResults()
    {
        if (File.Exists(VerificationReport))
        {
            string existingReport = File.ReadAllText(VerificationReport);
            int headingIndex = existingReport.IndexOf(
                ManualResultsHeading,
                StringComparison.Ordinal);
            if (headingIndex >= 0)
            {
                return existingReport.Substring(headingIndex).TrimEnd();
            }
        }

        StringBuilder pending = new StringBuilder();
        pending.AppendLine(ManualResultsHeading);
        pending.AppendLine();
        pending.AppendLine(
            "| Platform | Build | Independent runtime | Errors | Notes |");
        pending.AppendLine("|---|---|---|---:|---|");
        pending.AppendLine("| Windows | PENDING | PENDING |  |  |");
        pending.AppendLine("| WebGL | PENDING | PENDING |  |  |");
        pending.AppendLine("| Android | PENDING | PENDING |  |  |");
        pending.AppendLine();
        pending.AppendLine("## Release Decision");
        pending.AppendLine();
        pending.Append("PENDING");
        return pending.ToString();
    }

    private static void EnsureMainSceneIsFirst()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MainScene, true)
        };
    }

    private static bool IsMainSceneFirst()
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        return scenes.Length == 1 && scenes[0].enabled &&
               scenes[0].path == MainScene;
    }

    private static void Build(BuildTarget target, string outputPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { MainScene },
            locationPathName = outputPath,
            target = target,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        bool succeeded = report.summary.result == BuildResult.Succeeded;
        string summaryPath = target == BuildTarget.WebGL
            ? Path.Combine(outputPath, "build-summary.txt")
            : Path.Combine(Path.GetDirectoryName(outputPath), "build-summary.txt");
        File.WriteAllText(
            summaryPath,
            $"Result: {report.summary.result}\n" +
            $"Platform: {target}\n" +
            $"Duration: {report.summary.totalTime}\n" +
            $"Size: {report.summary.totalSize}\n" +
            $"Errors: {report.summary.totalErrors}\n" +
            $"Warnings: {report.summary.totalWarnings}\n");

        Debug.Log($"{target} build result: {report.summary.result}");
        ExitBatchMode(succeeded);
    }

    private static void ExitBatchMode(bool succeeded)
    {
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(succeeded ? 0 : 1);
        }
    }
}
