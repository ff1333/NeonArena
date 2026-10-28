using UnityEngine;

public sealed class CombatDirectorModel
{
    private readonly CombatDirectorConfig config;
    private float pressure;
    private float recoveryUntil;
    private float nextRecoveryAt;
    private bool recoveryArmed = true;
    private CombatDirectorOutput output;

    public CombatDirectorModel(CombatDirectorConfig directorConfig)
    {
        config = directorConfig;
        pressure = config.BasePressure;
        float progression = CalculateProgressionPressure(0f);
        output = BuildOutput(
            0f,
            progression,
            CalculateMinimumPressure(progression));
    }

    public CombatDirectorOutput Output => output;

    public CombatDirectorOutput Step(in CombatDirectorInput input)
    {
        float now = input.Now;
        bool recovering = now < recoveryUntil;

        if (input.PlayerHealthRatio >= config.RecoveryRearmHealthThreshold)
        {
            recoveryArmed = true;
        }

        if (!recovering &&
            recoveryArmed &&
            now >= nextRecoveryAt &&
            input.PlayerHealthRatio <= config.RecoveryHealthThreshold &&
            input.RecentDamageFraction >= config.HeavyDamageFraction)
        {
            recoveryUntil = now + config.RecoveryDuration;
            nextRecoveryAt = recoveryUntil + config.RecoveryCooldown;
            recoveryArmed = false;
            recovering = true;
        }

        if (recovering)
        {
            pressure = Mathf.MoveTowards(
                pressure,
                0f,
                config.PressureSmoothing * 2f * Mathf.Max(0f, input.DeltaTime));
        }
        else
        {
            float progression = CalculateProgressionPressure(input.ElapsedRunTime);
            float desired = CalculateDesiredPressure(input, progression);
            pressure = Mathf.MoveTowards(
                pressure,
                desired,
                config.PressureSmoothing * Mathf.Max(0f, input.DeltaTime));
        }

        pressure = Mathf.Clamp01(pressure);
        float currentProgression = CalculateProgressionPressure(input.ElapsedRunTime);
        output = BuildOutput(
            recovering ? Mathf.Max(0f, recoveryUntil - now) : 0f,
            currentProgression,
            CalculateMinimumPressure(currentProgression));
        return output;
    }

    private float CalculateProgressionPressure(float elapsedRunTime)
    {
        float progress = Mathf.Clamp01(
            Mathf.Max(0f, elapsedRunTime) /
            Mathf.Max(1f, config.TimeToMaximumProgression));
        return Mathf.Lerp(
            config.BasePressure,
            config.MaximumProgressionPressure,
            progress);
    }

    private float CalculateMinimumPressure(float progression)
    {
        return Mathf.Max(0f, progression - config.MaximumAdaptiveRelief);
    }

    private float CalculateDesiredPressure(
        in CombatDirectorInput input,
        float progression)
    {
        float health = Mathf.InverseLerp(
            config.HealthBoostThreshold,
            1f,
            Mathf.Clamp01(input.PlayerHealthRatio));
        float weapons = Mathf.Clamp01(input.WeaponCountRatio);
        float killRate = Mathf.Clamp01(
            input.KillsPerSecond / Mathf.Max(0.01f, config.BaselineKillsPerSecond) - 1f);
        int cap = Mathf.Max(1, input.CurrentEnemyCap);
        float crowd = Mathf.Clamp01(input.ActiveEnemies / (float)cap);
        float recentDamage = Mathf.Clamp01(
            input.RecentDamageFraction / Mathf.Max(0.01f, config.HeavyDamageFraction));

        float adaptive = health * config.HealthWeight +
                         weapons * config.WeaponWeight +
                         killRate * config.KillRateWeight -
                         crowd * config.CrowdWeight -
                         recentDamage * config.RecentDamageWeight;
        adaptive = Mathf.Clamp(
            adaptive,
            -config.MaximumAdaptiveRelief,
            config.MaximumAdaptiveBonus);

        return Mathf.Clamp(
            progression + adaptive,
            CalculateMinimumPressure(progression),
            1f);
    }

    private CombatDirectorOutput BuildOutput(
        float recoveryRemaining,
        float progressionPressure,
        float minimumPressure)
    {
        bool recovering = recoveryRemaining > 0f;
        float interval = Mathf.Lerp(
            config.SlowestSpawnInterval,
            config.FastestSpawnInterval,
            pressure);
        if (recovering)
        {
            interval *= config.RecoveryIntervalMultiplier;
        }

        int cap = Mathf.RoundToInt(Mathf.Lerp(
            config.MinimumEnemyCap,
            config.MaximumEnemyCap,
            pressure));
        if (recovering)
        {
            cap = config.MinimumEnemyCap;
        }

        int batch = 1 + Mathf.FloorToInt(
            pressure * Mathf.Max(0, config.MaximumBatch - 1));
        batch = Mathf.Clamp(batch, 1, config.MaximumBatch);
        if (recovering)
        {
            batch = 0;
        }

        int tier = Mathf.Clamp(
            Mathf.CeilToInt(pressure * 4f),
            0,
            4);

        return new CombatDirectorOutput(
            pressure,
            progressionPressure,
            minimumPressure,
            tier,
            interval,
            cap,
            batch,
            recovering,
            recoveryArmed,
            recovering,
            recoveryRemaining);
    }
}
