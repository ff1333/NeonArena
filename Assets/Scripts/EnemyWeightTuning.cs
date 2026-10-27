using System;
using UnityEngine;

[Serializable]
public sealed class EnemyWeightTuning
{
    [SerializeField] private EnemyBehaviorType behavior;
    [SerializeField, Min(0f)] private float calmMultiplier = 1f;
    [SerializeField, Min(0f)] private float pressureMultiplier = 1f;
    [SerializeField, Min(0f)] private float recoveryMultiplier = 1f;

    public EnemyBehaviorType Behavior => behavior;
    public float CalmMultiplier => calmMultiplier;
    public float PressureMultiplier => pressureMultiplier;
    public float RecoveryMultiplier => recoveryMultiplier;

    public EnemyWeightTuning()
    {
    }

    public EnemyWeightTuning(
        EnemyBehaviorType behaviorType,
        float calm,
        float pressured,
        float recovery)
    {
        behavior = behaviorType;
        calmMultiplier = Mathf.Max(0f, calm);
        pressureMultiplier = Mathf.Max(0f, pressured);
        recoveryMultiplier = Mathf.Max(0f, recovery);
    }
}
