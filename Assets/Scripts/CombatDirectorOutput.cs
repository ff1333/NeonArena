public readonly struct CombatDirectorOutput
{
    public CombatDirectorOutput(
        float pressure,
        int difficultyTier,
        float spawnInterval,
        int enemyCap,
        int batchCount,
        bool recovering,
        bool suppressSpawns,
        float recoveryRemaining)
    {
        Pressure = pressure;
        DifficultyTier = difficultyTier;
        SpawnInterval = spawnInterval;
        EnemyCap = enemyCap;
        BatchCount = batchCount;
        Recovering = recovering;
        SuppressSpawns = suppressSpawns;
        RecoveryRemaining = recoveryRemaining;
    }

    public float Pressure { get; }
    public int DifficultyTier { get; }
    public float SpawnInterval { get; }
    public int EnemyCap { get; }
    public int BatchCount { get; }
    public bool Recovering { get; }
    public bool SuppressSpawns { get; }
    public float RecoveryRemaining { get; }
}
