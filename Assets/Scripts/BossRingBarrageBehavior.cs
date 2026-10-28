using UnityEngine;

public sealed class BossRingBarrageBehavior : BossBehavior
{
    private float strikeAt;

    public BossRingBarrageBehavior(
        BossDecisionConfig config,
        BossBehaviorTuning tuning)
        : base(config, tuning)
    {
    }

    public override bool IsEligible(in BossSituation situation)
    {
        return Tuning.Enabled && situation.BossHealthRatio <= 0.5f;
    }

    public override float Evaluate(in BossSituation situation)
    {
        if (!IsEligible(situation))
        {
            return 0f;
        }

        float finalPressure = situation.IsFinal ? 0.2f : 0f;
        return Tuning.BaseWeight *
               RangePreference(situation.Distance) *
               (1f + finalPressure);
    }

    public override void Begin(BossAgent agent, in BossSituation situation)
    {
        strikeAt = Time.time + EffectiveWindup(situation);
        agent.HideAim();
        agent.ShowRing(2.4f);
    }

    public override bool Tick(BossAgent agent, float now)
    {
        if (now < strikeAt)
        {
            return false;
        }

        int count = agent.IsFinal ? 20 : 14;
        float speed = agent.IsFinal ? 7f : 6f;
        float damage = agent.IsFinal ? 22f : 18f;

        for (int i = 0; i < count; i++)
        {
            float angle = i * Mathf.PI * 2f / count;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            agent.FireHazard(
                (Vector2)agent.transform.position + direction * 1.4f,
                direction * speed,
                damage);
        }

        agent.HideRing();
        return true;
    }

    public override void Cancel(BossAgent agent)
    {
        agent.HideRing();
    }
}
