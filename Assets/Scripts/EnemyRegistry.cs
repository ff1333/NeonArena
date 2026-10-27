using System.Collections.Generic;
using UnityEngine;

public static class EnemyRegistry
{
    private static readonly List<EnemyController> activeEnemies =
        new List<EnemyController>();

    public static IReadOnlyList<EnemyController> ActiveEnemies => activeEnemies;
    public static int ActiveCount => activeEnemies.Count;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry()
    {
        activeEnemies.Clear();
    }

    public static void Register(EnemyController enemy)
    {
        if (enemy != null && !activeEnemies.Contains(enemy))
        {
            activeEnemies.Add(enemy);
        }
    }

    public static void Unregister(EnemyController enemy)
    {
        if (enemy != null)
        {
            activeEnemies.Remove(enemy);
        }
    }
}