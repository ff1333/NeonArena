using UnityEngine;

public abstract class BossBehavior
{
    private float lastStartedTime = float.NegativeInfinity;

    protected BossBehavior(
        BossDecisionConfig config,
        BossBehaviorTuning tuning)
    {
        Config = config;
        Tuning = tuning;
    }

    protected BossDecisionConfig Config { get; }
    public BossBehaviorTuning Tuning { get; }
    public BossActionType Type => Tuning.Type;
    public string DisplayName => Tuning.DisplayName;

    public float LastStartedTime => lastStartedTime;

    public float CooldownRemaining(float now)
    {
        return Mathf.Max(0f, lastStartedTime + Tuning.Cooldown - now);
    }

    public bool IsCoolingDown(float now)
    {
        return now < lastStartedTime + Tuning.Cooldown;
    }

    public void MarkStarted(float now)
    {
        lastStartedTime = now;
    }

    public virtual bool IsEligible(in BossSituation situation)
    {
        return Tuning.Enabled;
    }

    public abstract float Evaluate(in BossSituation situation);

    protected float RangePreference(float distance)
    {
        if (distance < Tuning.MinimumRange ||
            distance > Tuning.MaximumRange)
        {
            return 0.15f;
        }

        if (distance <= Tuning.IdealRange)
        {
            float span = Mathf.Max(0.01f, Tuning.IdealRange - Tuning.MinimumRange);
            return Mathf.Lerp(0.35f, 1f, (distance - Tuning.MinimumRange) / span);
        }

        float falloff = Mathf.Max(0.01f, Tuning.MaximumRange - Tuning.IdealRange);
        return Mathf.Lerp(1f, 0.35f, (distance - Tuning.IdealRange) / falloff);
    }

    protected float EffectiveWindup(in BossSituation situation)
    {
        if (situation.IsFinal && situation.IsEnraged)
        {
            return Tuning.Windup * Tuning.EnragedWindupScale;
        }

        return Tuning.Windup;
    }

    public abstract void Begin(BossAgent agent, in BossSituation situation);

    public abstract bool Tick(BossAgent agent, float now);

    public virtual void Cancel(BossAgent agent)
    {
    }
}
