using UnityEngine;

public sealed class BossZoneBombardmentBehavior : BossBehavior
{
    private float strikeAt;

    public BossZoneBombardmentBehavior(
        BossDecisionConfig config,
        BossBehaviorTuning tuning)
        : base(config, tuning)
    {
    }

    public override float Evaluate(in BossSituation situation)
    {
        float prediction = 1f + (1f - Mathf.Clamp01(situation.PlayerSpeed / 7f)) * 0.25f;
        float edgePressure = 0.7f + situation.PlayerEdge * 1.5f;
        return Tuning.BaseWeight *
               RangePreference(situation.Distance) *
               edgePressure *
               prediction;
    }

    public override void Begin(BossAgent agent, in BossSituation situation)
    {
        float windup = EffectiveWindup(situation);
        strikeAt = Time.time + windup;

        Vector2 target = agent.PredictPlayerPosition(0.4f);
        Vector2 clamped = agent.ClampPoint(target, 0.5f);
        Vector2 direction = (target - situation.BossPosition).normalized;
        if (direction.sqrMagnitude < 0.01f)
        {
            direction = Vector2.up;
        }

        if (agent.IsFinal)
        {
            agent.ArmZone(clamped, 3f, windup, 38f);
            Vector2 side = Vector2.Perpendicular(direction) * 4f;
            agent.ArmZone(agent.ClampPoint(clamped + side, 0.5f), 2.4f, windup + 0.25f, 38f);
            agent.ArmZone(agent.ClampPoint(clamped - side, 0.5f), 2.4f, windup + 0.25f, 38f);
        }
        else
        {
            agent.ArmZone(clamped, 2.3f, windup, 25f);
        }

        agent.HideAim();
        agent.HideRing();
    }

    public override bool Tick(BossAgent agent, float now)
    {
        return now >= strikeAt;
    }

    public override void Cancel(BossAgent agent)
    {
        agent.HideAim();
    }
}
