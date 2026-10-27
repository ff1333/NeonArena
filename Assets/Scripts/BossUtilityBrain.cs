using System.Collections.Generic;
using UnityEngine;

public sealed class BossUtilityBrain
{
    private readonly BossDecisionConfig config;
    private readonly List<BossBehavior> behaviors = new List<BossBehavior>(6);
    private readonly List<BossBehaviorScore> scores = new List<BossBehaviorScore>(6);

    public BossUtilityBrain(BossDecisionConfig decisionConfig)
    {
        config = decisionConfig;

        Add(new BossApproachBehavior(config, Require(BossActionType.Approach)));
        Add(new BossPredictiveDashBehavior(config, Require(BossActionType.PredictiveDash)));
        Add(new BossFanVolleyBehavior(config, Require(BossActionType.FanVolley)));
        Add(new BossZoneBombardmentBehavior(config, Require(BossActionType.ZoneBombardment)));
        Add(new BossRingBarrageBehavior(config, Require(BossActionType.RingBarrage)));
        Add(new BossRepositionBehavior(config, Require(BossActionType.Reposition)));
    }

    public BossDecision LastDecision { get; private set; }
    public BossActionType LastAction { get; private set; } = BossActionType.None;
    public int ConsecutiveRepeats { get; private set; }

    public BossDecision Evaluate(in BossSituation situation)
    {
        scores.Clear();
        BossBehavior best = null;
        float bestScore = float.NegativeInfinity;

        for (int i = 0; i < behaviors.Count; i++)
        {
            BossBehavior behavior = behaviors[i];
            bool eligible = behavior.IsEligible(situation);
            bool coolingDown = behavior.IsCoolingDown(situation.Time);
            bool available = eligible && !coolingDown;

            float rawScore = eligible ? behavior.Evaluate(situation) : 0f;
            float finalScore = rawScore;

            bool repeated =
                LastAction == behavior.Type &&
                situation.Time - behavior.LastStartedTime <= config.RepeatMemorySeconds;

            if (available && repeated)
            {
                float repeatPenalty = Mathf.Min(
                    config.RepeatPenalty * Mathf.Max(1, ConsecutiveRepeats),
                    config.MaximumRepeatPenalty);
                finalScore -= repeatPenalty;
            }

            if (!available)
            {
                finalScore = float.NegativeInfinity;
            }

            scores.Add(new BossBehaviorScore(
                behavior.Type,
                behavior.DisplayName,
                rawScore,
                finalScore,
                available,
                eligible,
                repeated,
                behavior.CooldownRemaining(situation.Time)));

            if (available && finalScore > bestScore)
            {
                bestScore = finalScore;
                best = behavior;
            }
        }

        BossActionType action = best != null ? best.Type : BossActionType.None;
        LastDecision = new BossDecision(
            action,
            best != null ? bestScore : 0f,
            scores.ToArray(),
            situation);
        return LastDecision;
    }

    public void NotifyStarted(BossActionType action)
    {
        if (action == LastAction)
        {
            ConsecutiveRepeats++;
        }
        else
        {
            LastAction = action;
            ConsecutiveRepeats = 1;
        }
    }

    public BossBehavior GetBehavior(BossActionType action)
    {
        for (int i = 0; i < behaviors.Count; i++)
        {
            if (behaviors[i].Type == action)
            {
                return behaviors[i];
            }
        }

        return null;
    }

    public float CooldownRemaining(BossActionType action, float now)
    {
        BossBehavior behavior = GetBehavior(action);
        return behavior != null ? behavior.CooldownRemaining(now) : 0f;
    }

    private void Add(BossBehavior behavior)
    {
        if (behavior != null)
        {
            behaviors.Add(behavior);
        }
    }

    private BossBehaviorTuning Require(BossActionType action)
    {
        BossBehaviorTuning tuning = config.Get(action);
        if (tuning == null)
        {
            Debug.LogError($"BossDecisionConfig is missing {action}.");
        }

        return tuning;
    }
}
