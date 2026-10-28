using System;
using UnityEngine;

[Serializable]
public class EnemySpawnEntry
{
    [SerializeField] private EnemyDefinition definition;
    [SerializeField, Min(0f)] private float weight = 1f;
    [SerializeField, Min(0f)] private float unlockTime;

    public EnemyDefinition Definition => definition;
    public float Weight => weight;
    public float UnlockTime => unlockTime;
}

public class EnemySpawner : MonoBehaviour
{
    [Header("Pools")]
    [SerializeField] private GameObjectPool enemyPool;
    [SerializeField] private GameObjectPool experiencePool;
    [SerializeField] private GameObjectPool warningPool;

    [Header("Enemy Selection")]
    [SerializeField] private EnemySpawnEntry[] enemyTypes;

    [Header("Difficulty")]
    [SerializeField, Min(0.1f)] private float startingInterval = 1.5f;
    [SerializeField, Min(0.1f)] private float minimumInterval = 0.65f;
    [SerializeField, Min(0f)] private float intervalDecreasePerSecond = 0.004f;

    [Header("Spawn Area")]
    [SerializeField] private ArenaBounds arenaBounds;
    [SerializeField, Min(0f)] private float arenaPadding = 0.75f;
    [SerializeField, Min(0f)] private float minimumDistanceFromPlayer = 5f;
    [SerializeField, Range(1, 128)] private int positionAttempts = 64;

    [Header("Warning")]
    [SerializeField, Min(0f)] private float warningDuration = 1.5f;

    private Transform player;
    private CombatDirector director;
    private float nextSpawnTime;
    private float runStartTime;

    public CombatDirector Director => director;

    private void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
        {
            Debug.LogError("EnemySpawner could not find the Player tag.", this);
            enabled = false;
            return;
        }

        player = playerObject.transform;

        if (arenaBounds == null || enemyPool == null ||
            experiencePool == null || warningPool == null ||
            enemyTypes == null || enemyTypes.Length == 0)
        {
            Debug.LogError(
                "EnemySpawner requires bounds, pools and enemy types.",
                this);
            enabled = false;
            return;
        }

        for (int i = 0; i < enemyTypes.Length; i++)
        {
            if (enemyTypes[i] == null || enemyTypes[i].Definition == null)
            {
                Debug.LogError(
                    $"EnemySpawner entry {i} has no definition.",
                    this);
                enabled = false;
                return;
            }
        }

        director = GetComponent<CombatDirector>();
        if (director == null)
        {
            director = gameObject.AddComponent<CombatDirector>();
        }

        director.Initialize(
            null,
            playerObject.GetComponent<Health>(),
            playerObject.GetComponent<PlayerShooter>());
    }

    private void OnEnable()
    {
        runStartTime = Time.time;
        nextSpawnTime = Time.time;
    }

    private void Update()
    {
        if (player == null)
        {
            return;
        }

        if (director != null && director.SpawnsSuppressed)
        {
            nextSpawnTime = Time.time + 0.25f;
            return;
        }

        if (Time.time < nextSpawnTime)
        {
            return;
        }

        float elapsed = Time.time - runStartTime;
        float timeInterval = Mathf.Max(
            minimumInterval,
            startingInterval - elapsed * intervalDecreasePerSecond);
        float currentInterval = director != null
            ? director.CurrentSpawnInterval
            : timeInterval;
        int cap = director != null ? director.CurrentEnemyCap : int.MaxValue;
        int requestedBatch = director != null ? director.CurrentBatch : 1;
        int allowedBatch = CombatDirector.ClampBatchToCapacity(
            EnemyRegistry.ActiveCount,
            cap,
            requestedBatch);

        if (allowedBatch <= 0)
        {
            nextSpawnTime = Time.time + 0.25f;
            return;
        }

        nextSpawnTime = Time.time + currentInterval;
        for (int i = 0; i < allowedBatch; i++)
        {
            SpawnOne(elapsed);
        }
    }

    private void SpawnOne(float elapsed)
    {
        EnemyDefinition selectedDefinition =
            ChooseEnemyDefinition(elapsed);
        if (selectedDefinition == null)
        {
            Debug.LogError(
                "No enemy type is eligible. Check unlock times and weights.",
                this);
            enabled = false;
            return;
        }

        Vector3 warningPosition = ChooseSpawnPosition();
        GameObject warningObject = warningPool.Get(
            warningPosition,
            Quaternion.identity);
        warningObject.GetComponent<EnemySpawnWarning>().Configure(
            player,
            enemyPool,
            experiencePool,
            selectedDefinition,
            warningDuration);
    }

    private EnemyDefinition ChooseEnemyDefinition(float elapsed)
    {
        float totalWeight = 0f;
        EnemyDefinition lastEligible = null;

        for (int i = 0; i < enemyTypes.Length; i++)
        {
            EnemySpawnEntry entry = enemyTypes[i];
            float weight = WeightFor(entry);
            if (elapsed >= entry.UnlockTime && weight > 0f)
            {
                totalWeight += weight;
                lastEligible = entry.Definition;
            }
        }

        if (totalWeight <= 0f)
        {
            return null;
        }

        float roll = UnityEngine.Random.Range(0f, totalWeight);

        for (int i = 0; i < enemyTypes.Length; i++)
        {
            EnemySpawnEntry entry = enemyTypes[i];
            float weight = WeightFor(entry);
            if (elapsed < entry.UnlockTime || weight <= 0f)
            {
                continue;
            }

            roll -= weight;
            if (roll <= 0f)
            {
                return entry.Definition;
            }
        }

        return lastEligible;
    }

    private float WeightFor(EnemySpawnEntry entry)
    {
        if (entry == null || entry.Definition == null)
        {
            return 0f;
        }

        float multiplier = director != null
            ? director.EnemyWeightMultiplier(entry.Definition.BehaviorType)
            : 1f;
        return entry.Weight * Mathf.Max(0f, multiplier);
    }

    private Vector3 ChooseSpawnPosition()
    {
        Vector2 min = arenaBounds.Minimum + Vector2.one * arenaPadding;
        Vector2 max = arenaBounds.Maximum - Vector2.one * arenaPadding;
        float minimumDistanceSqr =
            minimumDistanceFromPlayer * minimumDistanceFromPlayer;

        for (int attempt = 0; attempt < positionAttempts; attempt++)
        {
            Vector2 candidate = new Vector2(
                UnityEngine.Random.Range(min.x, max.x),
                UnityEngine.Random.Range(min.y, max.y));

            if ((candidate - (Vector2)player.position).sqrMagnitude >=
                minimumDistanceSqr)
            {
                return candidate;
            }
        }

        return ChooseFarthestCorner(min, max);
    }

    private Vector2 ChooseFarthestCorner(Vector2 min, Vector2 max)
    {
        Vector2[] corners =
        {
            new Vector2(min.x, min.y),
            new Vector2(min.x, max.y),
            new Vector2(max.x, min.y),
            new Vector2(max.x, max.y)
        };

        Vector2 farthest = corners[0];
        float farthestDistanceSqr =
            (farthest - (Vector2)player.position).sqrMagnitude;

        for (int i = 1; i < corners.Length; i++)
        {
            float distanceSqr =
                (corners[i] - (Vector2)player.position).sqrMagnitude;
            if (distanceSqr > farthestDistanceSqr)
            {
                farthest = corners[i];
                farthestDistanceSqr = distanceSqr;
            }
        }

        return farthest;
    }
}