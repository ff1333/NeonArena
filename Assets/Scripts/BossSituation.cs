using UnityEngine;

public readonly struct BossSituation
{
    public BossSituation(
        Vector2 bossPosition,
        Vector2 playerPosition,
        Vector2 playerVelocity,
        float bossHealthRatio,
        float playerEdge,
        float bossEdge,
        float time,
        bool isFinal)
    {
        BossPosition = bossPosition;
        PlayerPosition = playerPosition;
        PlayerVelocity = playerVelocity;
        BossHealthRatio = Mathf.Clamp01(bossHealthRatio);
        PlayerEdge = Mathf.Clamp01(playerEdge);
        BossEdge = Mathf.Clamp01(bossEdge);
        Time = time;
        IsFinal = isFinal;
    }

    public Vector2 BossPosition { get; }
    public Vector2 PlayerPosition { get; }
    public Vector2 PlayerVelocity { get; }
    public float BossHealthRatio { get; }
    public float PlayerEdge { get; }
    public float BossEdge { get; }
    public float Time { get; }
    public bool IsFinal { get; }

    public float Distance =>
        Vector2.Distance(BossPosition, PlayerPosition);
    public float PlayerSpeed => PlayerVelocity.magnitude;
    public bool IsEnraged => BossHealthRatio <= 0.5f;
}
