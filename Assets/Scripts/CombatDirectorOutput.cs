public readonly struct CombatDirectorOutput
{
    public CombatDirectorOutput(
        float pressure,
        float progressionPressure,
        float minimumPressure,
        int difficultyTier,
        float spawnInterval,
        int enemyCap,
        int batchCount,
        bool recovering,
        bool recoveryArmed,
        bool suppressSpawns,
        float recoveryRemaining)
    {
        Pressure = pressure;
        ProgressionPressure = progressionPressure;
        MinimumPressure = minimumPressure;
        DifficultyTier = difficultyTier;
        SpawnInterval = spawnInterval;
        EnemyCap = enemyCap;
        BatchCount = batchCount;
        Recovering = recovering;
        RecoveryArmed = recoveryArmed;
        SuppressSpawns = suppressSpawns;
        RecoveryRemaining = recoveryRemaining;
    }

    public float Pressure { get; }
    public float ProgressionPressure { get; }
    public float MinimumPressure { get; }
    public int DifficultyTier { get; }
    public float SpawnInterval { get; }
    public int EnemyCap { get; }
    public int BatchCount { get; }
    public bool Recovering { get; }
    public bool RecoveryArmed { get; }
    public bool SuppressSpawns { get; }
    public float RecoveryRemaining { get; }
}
