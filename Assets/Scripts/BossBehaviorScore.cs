public readonly struct BossBehaviorScore
{
    public BossBehaviorScore(
        BossActionType action,
        string displayName,
        float rawScore,
        float finalScore,
        bool available,
        bool eligible,
        bool repeated,
        float cooldownRemaining)
    {
        Action = action;
        DisplayName = displayName;
        RawScore = rawScore;
        FinalScore = finalScore;
        Available = available;
        Eligible = eligible;
        Repeated = repeated;
        CooldownRemaining = cooldownRemaining;
    }

    public BossActionType Action { get; }
    public string DisplayName { get; }
    public float RawScore { get; }
    public float FinalScore { get; }
    public bool Available { get; }
    public bool Eligible { get; }
    public bool Repeated { get; }
    public float CooldownRemaining { get; }
}
