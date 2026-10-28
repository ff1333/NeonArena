using UnityEngine;

public sealed class BossPredictiveDashBehavior : BossBehavior
{
    private Vector2 direction;
    private float strikeAt;
    private float dashEnd;
    private bool dashing;

    public BossPredictiveDashBehavior(
        BossDecisionConfig config,
        BossBehaviorTuning tuning)
        : base(config, tuning)
    {
    }

    public override float Evaluate(in BossSituation situation)
    {
        float speed = Mathf.Clamp01(situation.PlayerSpeed / 8f);
        return Tuning.BaseWeight *
               RangePreference(situation.Distance) *
               (0.75f + speed * 0.55f);
    }

    public override void Begin(BossAgent agent, in BossSituation situation)
    {
        Vector2 target = agent.PredictPlayerPosition(0.45f);
        direction = (target - situation.BossPosition).normalized;
        if (direction.sqrMagnitude < 0.01f)
        {
            direction = Vector2.up;
        }

        strikeAt = Time.time + EffectiveWindup(situation);
        dashEnd = 0f;
        dashing = false;
        agent.HideRing();
        agent.ShowAim(direction, agent.IsFinal ? 11f : 9f);
    }

    public override bool Tick(BossAgent agent, float now)
    {
        if (!dashing)
        {
            if (now < strikeAt)
            {
                return false;
            }

            dashing = true;
            agent.HideAim();
            dashEnd = now + 0.55f;
        }

        if (now < dashEnd)
        {
            agent.MoveInDirection(
                direction,
                agent.IsFinal ? 15f : 11f);
            return false;
        }

        return true;
    }

    public override void Cancel(BossAgent agent)
    {
        agent.HideAim();
    }
}
