using System.Collections.Generic;
using UnityEngine;

public sealed class CombatDirector : MonoBehaviour
{
    private sealed class DamageSample
    {
        public float Time;
        public float Amount;
    }

    private const float KillRateWindow = 8f;

    private readonly List<DamageSample> damageSamples = new List<DamageSample>(16);
    private readonly List<float> killTimes = new List<float>(32);
    private CombatDirectorConfig config;
    private CombatDirectorModel model;
    private Health playerHealth;
    private PlayerShooter shooter;

    public CombatDirectorConfig Config => config;
    public float Pressure => model != null ? model.Output.Pressure : 0f;
    public int DifficultyTier => model != null ? model.Output.DifficultyTier : 0;
    public float CurrentSpawnInterval =>
        model != null ? model.Output.SpawnInterval : 1.5f;
    public int CurrentEnemyCap =>
        model != null ? model.Output.EnemyCap : 10;
    public int CurrentBatch =>
        model != null ? model.Output.BatchCount : 1;
    public bool IsRecovering => model != null && model.Output.Recovering;
    public bool SpawnsSuppressed =>
        model != null && model.Output.SuppressSpawns;
    public float RecoveryRemaining =>
        model != null ? model.Output.RecoveryRemaining : 0f;
    public float RecentDamageFraction { get; private set; }

    public void Initialize(
        CombatDirectorConfig directorConfig,
        Health health,
        PlayerShooter playerShooter)
    {
        playerHealth = health;
        shooter = playerShooter;
        config = directorConfig != null
            ? directorConfig
            : Resources.Load<CombatDirectorConfig>("Combat/CombatDirectorConfig");

        if (config == null)
        {
            config = CombatDirectorConfig.CreateDefault();
        }

        model = new CombatDirectorModel(config);
        Subscribe();
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    private void Update()
    {
        if (model == null || playerHealth == null)
        {
            return;
        }

        float now = Time.time;
        TrimSamples(now);

        float maximum = Mathf.Max(1f, playerHealth.Max);
        float damageFraction = 0f;
        for (int i = 0; i < damageSamples.Count; i++)
        {
            damageFraction += damageSamples[i].Amount / maximum;
        }

        while (killTimes.Count > 0 && now - killTimes[0] > KillRateWindow)
        {
            killTimes.RemoveAt(0);
        }

        RecentDamageFraction = Mathf.Clamp01(damageFraction);

        float weaponRatio = shooter != null
            ? Mathf.Clamp01((shooter.EquippedCount - 1) / 5f)
            : 0f;

        CombatDirectorInput input = new CombatDirectorInput(
            now,
            Time.deltaTime,
            playerHealth.Max > 0f ? playerHealth.Current / playerHealth.Max : 0f,
            RecentDamageFraction,
            killTimes.Count / KillRateWindow,
            weaponRatio,
            EnemyRegistry.ActiveCount,
            CurrentEnemyCap);

        model.Step(input);
    }

    private void HandleDamaged(float amount)
    {
        damageSamples.Add(new DamageSample
        {
            Time = Time.time,
            Amount = Mathf.Max(0f, amount)
        });
    }

    private void HandleEnemyDied()
    {
        killTimes.Add(Time.time);
    }

    private void TrimSamples(float now)
    {
        float window = config != null ? config.RecentDamageWindow : 3f;
        for (int i = damageSamples.Count - 1; i >= 0; i--)
        {
            if (now - damageSamples[i].Time > window)
            {
                damageSamples.RemoveAt(i);
            }
        }
    }

    public float EnemyWeightMultiplier(EnemyBehaviorType behavior)
    {
        return config != null
            ? config.GetWeightMultiplier(behavior, Pressure, IsRecovering)
            : 1f;
    }

    public static int ClampBatchToCapacity(int activeEnemies, int enemyCap, int requested)
    {
        int availableSlots = Mathf.Max(0, enemyCap - Mathf.Max(0, activeEnemies));
        return Mathf.Clamp(requested, 0, availableSlots);
    }

    private void Subscribe()
    {
        if (playerHealth != null)
        {
            playerHealth.Damaged -= HandleDamaged;
            playerHealth.Damaged += HandleDamaged;
        }

        EnemyController.DiedGlobally -= HandleEnemyDied;
        EnemyController.DiedGlobally += HandleEnemyDied;
    }

    private void Unsubscribe()
    {
        if (playerHealth != null)
        {
            playerHealth.Damaged -= HandleDamaged;
        }

        EnemyController.DiedGlobally -= HandleEnemyDied;
    }
}
