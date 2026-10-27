using UnityEngine;

public sealed class BossApproachBehavior : BossBehavior
{
    private float endTime;

    public BossApproachBehavior(
        BossDecisionConfig config,
        BossBehaviorTuning tuning)
        : base(config, tuning)
    {
    }

    public override float Evaluate(in BossSituation situation)
    {
        float far = Mathf.InverseLerp(3.5f, 13f, situation.Distance);
        return Tuning.BaseWeight * (0.25f + far * 1.75f);
    }

    public override void Begin(BossAgent agent, in BossSituation situation)
    {
        endTime = Time.time + 0.75f;
        agent.HideAim();
        agent.HideRing();
    }

    public override bool Tick(BossAgent agent, float now)
    {
        if (now >= endTime)
        {
            return true;
        }

        agent.MoveTowardPlayer(agent.IsFinal ? 7f : 5.4f);
        return false;
    }
}
