using System.Collections.Generic;

public sealed class BossDecision
{
    public BossDecision(
        BossActionType action,
        float score,
        IReadOnlyList<BossBehaviorScore> scores,
        BossSituation situation)
    {
        Action = action;
        Score = score;
        Scores = scores;
        Situation = situation;
    }

    public BossActionType Action { get; }
    public float Score { get; }
    public IReadOnlyList<BossBehaviorScore> Scores { get; }
    public BossSituation Situation { get; }
    public bool HasAction => Action != BossActionType.None;
}
