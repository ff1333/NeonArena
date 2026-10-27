using UnityEngine;

public sealed class CombatDirectorModel
{
    private readonly CombatDirectorConfig config;
    private float pressure;
    private float recoveryUntil;
    private float nextRecoveryAt;
    private CombatDirectorOutput output;

    public CombatDirectorModel(CombatDirectorConfig directorConfig)
    {
        config = directorConfig;
        pressure = config.BasePressure;
        output = BuildOutput(0f);
    }

    public CombatDirectorOutput Output => output;

    public CombatDirectorOutput Step(in CombatDirectorInput input)
    {
        float now = input.Now;
        bool recovering = now < recoveryUntil;

        if (!recovering &&
            now >= nextRecoveryAt &&
            input.PlayerHealthRatio <= config.RecoveryHealthThreshold &&
            input.RecentDamageFraction >= config.HeavyDamageFraction)
        {
            recoveryUntil = now + config.RecoveryDuration;
            nextRecoveryAt = recoveryUntil + config.RecoveryCooldown;
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
            float desired = CalculateDesiredPressure(input);
            pressure = Mathf.MoveTowards(
                pressure,
                desired,
                config.PressureSmoothing * Mathf.Max(0f, input.DeltaTime));
        }

        pressure = Mathf.Clamp01(pressure);
        output = BuildOutput(recovering ? Mathf.Max(0f, recoveryUntil - now) : 0f);
        return output;
    }

    private float CalculateDesiredPressure(in CombatDirectorInput input)
    {
        float health = (Mathf.Clamp01(input.PlayerHealthRatio) - 0.5f) * 2f;
        float weapons = (Mathf.Clamp01(input.WeaponCountRatio) - 0.5f) * 2f;
        float killRate = Mathf.Clamp01(
            input.KillsPerSecond / Mathf.Max(0.01f, config.BaselineKillsPerSecond) - 1f);
        int cap = Mathf.Max(1, input.CurrentEnemyCap);
        float crowd = Mathf.Clamp01(input.ActiveEnemies / (float)cap);
        float recentDamage = Mathf.Clamp01(
            input.RecentDamageFraction / Mathf.Max(0.01f, config.HeavyDamageFraction));

        float desired = config.BasePressure +
                        health * config.HealthWeight +
                        weapons * config.WeaponWeight +
                        killRate * config.KillRateWeight -
                        crowd * config.CrowdWeight -
                        recentDamage * config.RecentDamageWeight;
        return Mathf.Clamp01(desired);
    }

    private CombatDirectorOutput BuildOutput(float recoveryRemaining)
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
            tier,
            interval,
            cap,
            batch,
            recovering,
            recovering,
            recoveryRemaining);
    }
}
