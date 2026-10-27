using System;
using UnityEngine;

[Serializable]
public sealed class BossBehaviorTuning
{
    [SerializeField] private BossActionType type;
    [SerializeField] private string displayName = "Action";
    [SerializeField] private bool enabled = true;
    [SerializeField, Min(0f)] private float baseWeight = 1f;
    [SerializeField, Min(0f)] private float minimumRange;
    [SerializeField, Min(0f)] private float idealRange = 6f;
    [SerializeField, Min(0f)] private float maximumRange = 14f;
    [SerializeField, Min(0f)] private float cooldown = 4f;
    [SerializeField, Min(0.05f)] private float windup = 0.85f;
    [SerializeField, Min(0f)] private float recovery = 0.35f;
    [SerializeField, Range(0.1f, 1f)] private float enragedWindupScale = 0.75f;

    public BossActionType Type => type;
    public string DisplayName => displayName;
    public bool Enabled => enabled;
    public float BaseWeight => baseWeight;
    public float MinimumRange => minimumRange;
    public float IdealRange => idealRange;
    public float MaximumRange => maximumRange;
    public float Cooldown => cooldown;
    public float Windup => windup;
    public float Recovery => recovery;
    public float EnragedWindupScale => enragedWindupScale;

    public BossBehaviorTuning()
    {
    }

    public BossBehaviorTuning(
        BossActionType actionType,
        string label,
        float weight,
        float cooldownSeconds,
        float windupSeconds,
        float recoverySeconds,
        float minimum,
        float ideal,
        float maximum)
    {
        type = actionType;
        displayName = label;
        baseWeight = Mathf.Max(0f, weight);
        cooldown = Mathf.Max(0f, cooldownSeconds);
        windup = Mathf.Max(0.05f, windupSeconds);
        recovery = Mathf.Max(0f, recoverySeconds);
        minimumRange = Mathf.Max(0f, minimum);
        idealRange = Mathf.Max(minimumRange, ideal);
        maximumRange = Mathf.Max(idealRange, maximum);
    }
}
