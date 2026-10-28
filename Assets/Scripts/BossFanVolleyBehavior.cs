using UnityEngine;

public sealed class BossFanVolleyBehavior : BossBehavior
{
    private Vector2 direction;
    private float strikeAt;

    public BossFanVolleyBehavior(
        BossDecisionConfig config,
        BossBehaviorTuning tuning)
        : base(config, tuning)
    {
    }

    public override float Evaluate(in BossSituation situation)
    {
        float calm = 1f - Mathf.Clamp01(situation.PlayerSpeed / 8f);
        return Tuning.BaseWeight *
               RangePreference(situation.Distance) *
               (1f + calm * 0.28f);
    }

    public override void Begin(BossAgent agent, in BossSituation situation)
    {
        Vector2 target = agent.PredictPlayerPosition(0.3f);
        direction = (target - situation.BossPosition).normalized;
        if (direction.sqrMagnitude < 0.01f)
        {
            direction = Vector2.up;
        }

        strikeAt = Time.time + EffectiveWindup(situation);
        agent.HideRing();
        agent.ShowAim(direction, 12f);
    }

    public override bool Tick(BossAgent agent, float now)
    {
        if (now < strikeAt)
        {
            return false;
        }

        int count = agent.IsFinal ? 7 : 3;
        float spread = agent.IsFinal ? 12f : 11f;
        float speed = agent.IsFinal ? 9f : 7f;
        float damage = agent.IsFinal ? 28f : 18f;

        for (int i = 0; i < count; i++)
        {
            float angle = (i - (count - 1) * 0.5f) * spread;
            Vector2 shotDirection =
                Quaternion.Euler(0f, 0f, angle) * direction;
            agent.FireHazard(
                (Vector2)agent.transform.position + shotDirection * 1.4f,
                shotDirection * speed,
                damage);
        }

        agent.HideAim();
        return true;
    }

    public override void Cancel(BossAgent agent)
    {
        agent.HideAim();
    }
}
