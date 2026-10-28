using UnityEngine;

public sealed class BossRepositionBehavior : BossBehavior
{
    private Vector2 start;
    private Vector2 destination;
    private float endTime;
    private float duration;

    public BossRepositionBehavior(
        BossDecisionConfig config,
        BossBehaviorTuning tuning)
        : base(config, tuning)
    {
    }

    public override float Evaluate(in BossSituation situation)
    {
        float close = 1f - Mathf.InverseLerp(2f, 7f, situation.Distance);
        return Tuning.BaseWeight *
               (0.35f + close * 1.3f + situation.BossEdge * 0.9f);
    }

    public override void Begin(BossAgent agent, in BossSituation situation)
    {
        start = situation.BossPosition;
        Vector2 away = (situation.BossPosition - situation.PlayerPosition).normalized;
        if (away.sqrMagnitude < 0.01f)
        {
            away = Vector2.up;
        }

        float side = Vector2.Dot(away, Vector2.right) >= 0f ? 1f : -1f;
        Vector2 destinationDirection =
            (away * 0.6f + Vector2.Perpendicular(away) * side * 0.8f).normalized;
        destination = agent.ClampPoint(
            situation.BossPosition + destinationDirection * 6f,
            1.4f);

        duration = 0.45f;
        endTime = Time.time + duration;
        agent.HideAim();
        agent.HideRing();
    }

    public override bool Tick(BossAgent agent, float now)
    {
        if (now >= endTime)
        {
            agent.MovePosition(destination);
            return true;
        }

        float elapsed = 1f - (endTime - now) / duration;
        agent.MovePosition(Vector2.Lerp(start, destination, Mathf.Clamp01(elapsed)));
        return false;
    }

    public override void Cancel(BossAgent agent)
    {
        agent.HideAim();
        agent.HideRing();
    }
}
