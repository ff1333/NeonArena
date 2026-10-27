#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class CombatDebugOverlay : MonoBehaviour
{
    private BossEncounter encounter;
    private EnemySpawner spawner;
    private GUIStyle titleStyle;
    private GUIStyle bodyStyle;
    private bool visible = true;
    private float nextRefresh;

    private void Awake()
    {
        RefreshReferences();
    }

    private void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.f3Key.wasPressedThisFrame)
        {
            visible = !visible;
        }

        if (Time.unscaledTime >= nextRefresh)
        {
            nextRefresh = Time.unscaledTime + 1f;
            RefreshReferences();
        }
    }

    private void RefreshReferences()
    {
        if (encounter == null)
        {
            encounter = GetComponent<BossEncounter>();
        }

        if (spawner == null)
        {
            spawner = FindFirstObjectByType<EnemySpawner>();
        }
    }

    private void OnGUI()
    {
        if (!visible)
        {
            return;
        }

        EnsureStyles();
        BossAgent boss = encounter != null ? encounter.ActiveBoss : null;
        CombatDirector director = spawner != null ? spawner.Director : null;

        float width = 520f;
        float height = 250f;
        GUILayout.BeginArea(new Rect(12f, 12f, width, height), GUI.skin.box);
        GUILayout.Label("NEON ARENA // COMBAT AI DEBUG  (F3)", titleStyle);

        if (boss == null)
        {
            GUILayout.Label("BOSS  inactive", bodyStyle);
        }
        else
        {
            GUILayout.Label(
                $"BOSS  action={boss.CurrentAction}  last={boss.LastAction}  " +
                $"hp={(boss.Health.Max > 0f ? boss.Health.Current / boss.Health.Max : 0f):P0}",
                bodyStyle);

            BossDecision decision = boss.LastDecision;
            if (decision != null)
            {
                List<BossBehaviorScore> ordered =
                    new List<BossBehaviorScore>(decision.Scores);
                ordered.Sort((a, b) => b.FinalScore.CompareTo(a.FinalScore));
                int count = Mathf.Min(4, ordered.Count);
                for (int i = 0; i < count; i++)
                {
                    BossBehaviorScore score = ordered[i];
                    string cooldown = score.Available
                        ? string.Empty
                        : $"  cd {score.CooldownRemaining:0.0}";
                    string repeat = score.Repeated ? "  repeat" : string.Empty;
                    GUILayout.Label(
                        $"  {score.DisplayName,-20} {score.FinalScore,6:0.00}" +
                        $"{cooldown}{repeat}",
                        bodyStyle);
                }
            }
        }

        if (director == null)
        {
            GUILayout.Label("DIRECTOR  inactive", bodyStyle);
        }
        else
        {
            GUILayout.Label(
                $"DIRECTOR  pressure={director.Pressure:0.00}  tier={director.DifficultyTier}  " +
                $"interval={director.CurrentSpawnInterval:0.00}s  cap={director.CurrentEnemyCap}  " +
                $"batch={director.CurrentBatch}",
                bodyStyle);
            GUILayout.Label(
                $"  damage={director.RecentDamageFraction:P0}  " +
                $"recovery={director.RecoveryRemaining:0.0}s  " +
                $"active={EnemyRegistry.ActiveCount}",
                bodyStyle);
        }

        GUILayout.EndArea();
    }

    private void EnsureStyles()
    {
        if (titleStyle != null)
        {
            return;
        }

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 0.72f, 0.3f) }
        };
        bodyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            normal = { textColor = Color.white }
        };
    }
}
#endif
