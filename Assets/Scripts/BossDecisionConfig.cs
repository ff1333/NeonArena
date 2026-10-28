using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Neon Arena/Boss Decision Config")]
public sealed class BossDecisionConfig : ScriptableObject
{
    [Header("Candidate Actions")]
    [SerializeField] private BossBehaviorTuning[] behaviors = CreateDefaultTunings();

    [Header("Shared Scoring")]
    [SerializeField, Min(0f)] private float repeatPenalty = 2.4f;
    [SerializeField, Min(0f)] private float maximumRepeatPenalty = 4.8f;
    [SerializeField, Min(0f)] private float repeatMemorySeconds = 6f;

    public BossBehaviorTuning[] Behaviors => behaviors;

    public BossBehaviorTuning Get(BossActionType type)
    {
        if (behaviors == null)
        {
            return null;
        }

        for (int i = 0; i < behaviors.Length; i++)
        {
            if (behaviors[i] != null && behaviors[i].Type == type)
            {
                return behaviors[i];
            }
        }

        return null;
    }

    public float RepeatPenalty => repeatPenalty;
    public float MaximumRepeatPenalty => maximumRepeatPenalty;
    public float RepeatMemorySeconds => repeatMemorySeconds;

    public static BossDecisionConfig CreateDefault()
    {
        BossDecisionConfig config = CreateInstance<BossDecisionConfig>();
        config.behaviors = CreateDefaultTunings();
        return config;
    }

    private static BossBehaviorTuning[] CreateDefaultTunings()
    {
        return new[]
        {
            new BossBehaviorTuning(
                BossActionType.Approach, "PURSUIT", 1.2f, 1.4f, 0.1f, 0.4f,
                0.5f, 4f, 20f),
            new BossBehaviorTuning(
                BossActionType.PredictiveDash, "PREDICTIVE DASH", 2.1f, 4.5f,
                0.8f, 0.4f, 3f, 6.5f, 12f),
            new BossBehaviorTuning(
                BossActionType.FanVolley, "FAN VOLLEY", 2.0f, 3.2f, 0.8f, 0.35f,
                3f, 7f, 14f),
            new BossBehaviorTuning(
                BossActionType.ZoneBombardment, "ZONE BOMBARDMENT", 1.6f, 5.5f,
                1.0f, 0.45f, 2.5f, 7f, 16f),
            new BossBehaviorTuning(
                BossActionType.RingBarrage, "RING BARRAGE", 2.6f, 6.5f, 0.75f, 0.4f,
                1.5f, 4.5f, 12f),
            new BossBehaviorTuning(
                BossActionType.Reposition, "REPOSITION", 1.0f, 5f, 0.1f, 0.35f,
                0f, 3f, 9f)
        };
    }
}
