public readonly struct CombatDirectorInput
{
    public CombatDirectorInput(
        float now,
        float deltaTime,
        float elapsedRunTime,
        float playerHealthRatio,
        float recentDamageFraction,
        float killsPerSecond,
        float weaponCountRatio,
        int activeEnemies,
        int currentEnemyCap)
    {
        Now = now;
        DeltaTime = deltaTime;
        ElapsedRunTime = elapsedRunTime;
        PlayerHealthRatio = playerHealthRatio;
        RecentDamageFraction = recentDamageFraction;
        KillsPerSecond = killsPerSecond;
        WeaponCountRatio = weaponCountRatio;
        ActiveEnemies = activeEnemies;
        CurrentEnemyCap = currentEnemyCap;
    }

    public float Now { get; }
    public float DeltaTime { get; }
    public float ElapsedRunTime { get; }
    public float PlayerHealthRatio { get; }
    public float RecentDamageFraction { get; }
    public float KillsPerSecond { get; }
    public float WeaponCountRatio { get; }
    public int ActiveEnemies { get; }
    public int CurrentEnemyCap { get; }
}
