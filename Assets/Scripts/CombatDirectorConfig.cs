using UnityEngine;

[CreateAssetMenu(menuName = "Neon Arena/Combat Director Config")]
public sealed class CombatDirectorConfig : ScriptableObject
{
    [Header("Progression Pressure")]
    [SerializeField, Range(0f, 1f)] private float basePressure = 0.15f;
    [SerializeField, Range(0f, 1f)] private float maximumProgressionPressure = 0.75f;
    [SerializeField, Min(1f)] private float timeToMaximumProgression = 600f;
    [SerializeField, Range(0f, 1f)] private float maximumAdaptiveBonus = 0.25f;
    [SerializeField, Range(0f, 1f)] private float maximumAdaptiveRelief = 0.15f;

    [Header("Adaptive Pressure Inputs")]
    [SerializeField, Range(0f, 1f)] private float healthBoostThreshold = 0.6f;
    [SerializeField, Min(0f)] private float healthWeight = 0.1f;
    [SerializeField, Min(0f)] private float weaponWeight = 0.18f;
    [SerializeField, Min(0f)] private float killRateWeight = 0.22f;
    [SerializeField, Min(0f)] private float crowdWeight = 0.2f;
    [SerializeField, Min(0f)] private float recentDamageWeight = 0.35f;
    [SerializeField, Min(0.01f)] private float baselineKillsPerSecond = 0.25f;

    [Header("Damage Window")]
    [SerializeField, Min(0.5f)] private float recentDamageWindow = 3f;
    [SerializeField, Range(0.05f, 1f)] private float heavyDamageFraction = 0.18f;

    [Header("Recovery")]
    [SerializeField, Range(0.05f, 0.95f)] private float recoveryHealthThreshold = 0.35f;
    [SerializeField, Range(0.05f, 1f)] private float recoveryRearmHealthThreshold = 0.55f;
    [SerializeField, Min(0.5f)] private float recoveryDuration = 3.5f;
    [SerializeField, Min(0f)] private float recoveryCooldown = 7f;

    [Header("Bounded Output")]
    [SerializeField, Min(0.05f)] private float pressureSmoothing = 1.2f;
    [SerializeField, Min(0.1f)] private float slowestSpawnInterval = 1.5f;
    [SerializeField, Min(0.1f)] private float fastestSpawnInterval = 0.55f;
    [SerializeField, Range(1, 64)] private int minimumEnemyCap = 10;
    [SerializeField, Range(1, 64)] private int maximumEnemyCap = 28;
    [SerializeField, Range(1, 8)] private int maximumBatch = 3;
    [SerializeField, Min(1f)] private float recoveryIntervalMultiplier = 1.75f;

    [Header("Enemy Composition")]
    [SerializeField] private EnemyWeightTuning[] enemyWeights =
        CreateDefaultEnemyWeights();

    public float BasePressure => basePressure;
    public float MaximumProgressionPressure => maximumProgressionPressure;
    public float TimeToMaximumProgression => timeToMaximumProgression;
    public float MaximumAdaptiveBonus => maximumAdaptiveBonus;
    public float MaximumAdaptiveRelief => maximumAdaptiveRelief;
    public float HealthBoostThreshold => healthBoostThreshold;
    public float HealthWeight => healthWeight;
    public float WeaponWeight => weaponWeight;
    public float KillRateWeight => killRateWeight;
    public float CrowdWeight => crowdWeight;
    public float RecentDamageWeight => recentDamageWeight;
    public float BaselineKillsPerSecond => baselineKillsPerSecond;
    public float RecentDamageWindow => recentDamageWindow;
    public float HeavyDamageFraction => heavyDamageFraction;
    public float RecoveryHealthThreshold => recoveryHealthThreshold;
    public float RecoveryRearmHealthThreshold => recoveryRearmHealthThreshold;
    public float RecoveryDuration => recoveryDuration;
    public float RecoveryCooldown => recoveryCooldown;
    public float PressureSmoothing => pressureSmoothing;
    public float SlowestSpawnInterval => slowestSpawnInterval;
    public float FastestSpawnInterval => fastestSpawnInterval;
    public int MinimumEnemyCap => minimumEnemyCap;
    public int MaximumEnemyCap => maximumEnemyCap;
    public int MaximumBatch => maximumBatch;
    public float RecoveryIntervalMultiplier => recoveryIntervalMultiplier;
    public EnemyWeightTuning[] EnemyWeights => enemyWeights;

    public float GetWeightMultiplier(
        EnemyBehaviorType behavior,
        float pressure,
        bool recovering)
    {
        EnemyWeightTuning tuning = FindWeight(behavior);
        if (tuning == null)
        {
            return 1f;
        }

        if (recovering)
        {
            return tuning.RecoveryMultiplier;
        }

        return Mathf.Lerp(
            tuning.CalmMultiplier,
            tuning.PressureMultiplier,
            Mathf.Clamp01(pressure));
    }

    public static CombatDirectorConfig CreateDefault()
    {
        CombatDirectorConfig config = CreateInstance<CombatDirectorConfig>();
        config.enemyWeights = CreateDefaultEnemyWeights();
        return config;
    }

    private EnemyWeightTuning FindWeight(EnemyBehaviorType behavior)
    {
        if (enemyWeights == null)
        {
            return null;
        }

        for (int i = 0; i < enemyWeights.Length; i++)
        {
            if (enemyWeights[i] != null &&
                enemyWeights[i].Behavior == behavior)
            {
                return enemyWeights[i];
            }
        }

        return null;
    }

    private static EnemyWeightTuning[] CreateDefaultEnemyWeights()
    {
        return new[]
        {
            new EnemyWeightTuning(EnemyBehaviorType.Chaser, 1.25f, 0.85f, 1.6f),
            new EnemyWeightTuning(EnemyBehaviorType.Dasher, 0.65f, 1.2f, 0.25f),
            new EnemyWeightTuning(EnemyBehaviorType.Berserker, 0.45f, 1.4f, 0.15f)
        };
    }
}
