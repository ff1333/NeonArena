using UnityEngine;

public sealed class BossAgent : MonoBehaviour
{
    private BossEncounter encounter;
    private Health player;
    private Rigidbody2D playerBody;
    private ArenaBounds bounds;
    private Rigidbody2D body;
    private BossDecisionConfig decisionConfig;
    private BossUtilityBrain brain;
    private BossBehavior activeBehavior;
    private BossBehaviorTuning activeTuning;
    private BossActionType currentAction;
    private BossActionType lastAction;
    private readonly BossHazard[] hazards = new BossHazard[48];
    private LineRenderer aim;
    private LineRenderer ringAim;
    private Vector2 lockedDirection;
    private float nextDamage, nextDecisionAt;
    private bool defeated;

    public bool IsFinal { get; private set; }
    public Health Health { get; private set; }
    public bool IsPreparing => activeBehavior != null;
    public BossActionType CurrentAction => currentAction;
    public Vector2 LockedDirection => lockedDirection;
    public BossActionType LastAction => lastAction;
    public BossActionType LastCompletedAction { get; private set; } = BossActionType.None;
#if UNITY_EDITOR
    public bool DebugHoldDecisions { get; set; }
#endif
    public BossDecision LastDecision => brain?.LastDecision;
    public BossDecisionConfig DecisionConfig => decisionConfig;

    public void Initialize(
        BossEncounter owner,
        Health target,
        ArenaBounds arena,
        bool final,
        float maximum)
    {
        encounter = owner;
        player = target;
        playerBody = player.GetComponent<Rigidbody2D>();
        bounds = arena;
        IsFinal = final;
        body = GetComponent<Rigidbody2D>();
        Health = GetComponent<Health>();
        GetComponent<EnemyController>().SpawnBoss(this, maximum, Color.white);

        BuildTelegraphs();
        BuildHazardPool();

        decisionConfig = Resources.Load<BossDecisionConfig>("Combat/BossDecisionConfig");
        if (decisionConfig == null || decisionConfig.Behaviors == null ||
            decisionConfig.Behaviors.Length == 0)
        {
            decisionConfig = BossDecisionConfig.CreateDefault();
        }

        brain = new BossUtilityBrain(decisionConfig);
        currentAction = BossActionType.None;
        lastAction = BossActionType.None;
        nextDecisionAt = Time.time + 1.2f;
    }

    private void BuildTelegraphs()
    {
        GameObject aimRoot = new GameObject("Attack Telegraph");
        aimRoot.transform.SetParent(transform, false);
        aim = aimRoot.AddComponent<LineRenderer>();
        aim.sharedMaterial = ArenaPresentation.TrailMaterial;
        aim.useWorldSpace = true;
        aim.startWidth = aim.endWidth = 0.14f;
        aim.startColor = aim.endColor = new Color(1f, 0.3f, 0.16f, 0.8f);
        aim.sortingOrder = 10;
        aim.positionCount = 2;
        aim.enabled = false;

        GameObject ringRoot = new GameObject("Ring Telegraph");
        ringRoot.transform.SetParent(transform, false);
        ringAim = ringRoot.AddComponent<LineRenderer>();
        ringAim.enabled = false;
    }

    private void BuildHazardPool()
    {
        for (int i = 0; i < hazards.Length; i++)
        {
            GameObject root = new GameObject("Boss Hazard " + i);
            root.SetActive(false);
            root.transform.SetParent(encounter.transform);
            hazards[i] = root.AddComponent<BossHazard>();
            hazards[i].Initialize(this, player, bounds);
        }
    }

    private void FixedUpdate()
    {
        if (player == null || player.IsDead || Health == null ||
            Health.IsDead || defeated)
        {
            CancelActiveBehavior();
            return;
        }

        Vector2 position = body.position;
        if (Vector2.Distance(position, player.transform.position) <
            (IsFinal ? 1.55f : 1.25f))
        {
            TryDamage(IsFinal ? 34f : 24f);
        }

        if (activeBehavior != null)
        {
            if (activeBehavior.Tick(this, Time.time))
            {
                CompleteActiveBehavior();
            }

            return;
        }

        if (Time.time >= nextDecisionAt)
        {
            StartBestAction();
            return;
        }

        MoveTowardPlayer(IsFinal ? 3.4f : 2.3f);
    }

    private void StartBestAction()
    {
        BossSituation situation = BuildSituation();
        BossDecision decision = brain.Evaluate(situation);
        if (!decision.HasAction)
        {
            nextDecisionAt = Time.time + 0.2f;
            return;
        }

        activeBehavior = brain.GetBehavior(decision.Action);
        if (activeBehavior == null)
        {
            nextDecisionAt = Time.time + 0.2f;
            return;
        }

        activeTuning = activeBehavior.Tuning;
        currentAction = decision.Action;
        lastAction = decision.Action;
        activeBehavior.MarkStarted(Time.time);
        brain.NotifyStarted(decision.Action);
        activeBehavior.Begin(this, situation);
        CombatFeedback.Instance?.PlayEnemyCharge(
            transform.position,
            new Color(1f, 0.4f, 0.2f));
    }

    private void CompleteActiveBehavior()
    {
        if (activeBehavior != null)
        {
            activeBehavior.Cancel(this);
        }

        LastCompletedAction = currentAction;
        float recovery = activeTuning != null ? activeTuning.Recovery : 0.35f;
        activeBehavior = null;
        activeTuning = null;
        currentAction = BossActionType.None;
        nextDecisionAt = DebugDecisionsHeld ? float.PositiveInfinity : Time.time + recovery;
        HideAim();
        HideRing();
    }

    private void CancelActiveBehavior()
    {
        if (activeBehavior != null)
        {
            activeBehavior.Cancel(this);
        }

        activeBehavior = null;
        activeTuning = null;
        currentAction = BossActionType.None;
    }

    private BossSituation BuildSituation()
    {
        Vector2 playerPosition = player.transform.position;
        Vector2 playerVelocity =
            playerBody != null ? playerBody.linearVelocity : Vector2.zero;
        float healthRatio = Health.Max > 0f ? Health.Current / Health.Max : 1f;

        return new BossSituation(
            body.position,
            playerPosition,
            playerVelocity,
            healthRatio,
            bounds.EdgeProximity(playerPosition),
            bounds.EdgeProximity(body.position),
            Time.time,
            IsFinal);
    }

    public Vector2 PredictPlayerPosition(float leadTime)
    {
        Vector2 velocity = playerBody != null ? playerBody.linearVelocity : Vector2.zero;
        Vector2 target = (Vector2)player.transform.position +
                         Vector2.ClampMagnitude(velocity, 9f) * leadTime;
        return bounds.ClampPoint(target, 0.5f);
    }

    public Vector2 ClampPoint(Vector2 point, float padding)
    {
        return bounds.ClampPoint(point, padding);
    }

    public void MovePosition(Vector2 position)
    {
        body.MovePosition(bounds.ClampPoint(position, 1.4f));
    }

    public void MoveTowardPlayer(float speed)
    {
        Vector2 direction =
            ((Vector2)player.transform.position - body.position).normalized;
        MoveInDirection(direction, speed);
    }

    public void MoveInDirection(Vector2 direction, float speed)
    {
        MovePosition(body.position + direction * speed * Time.fixedDeltaTime);
    }

    public void ShowAim(Vector2 direction, float length)
    {
        lockedDirection = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector2.up;
        aim.enabled = true;
        aim.SetPosition(0, transform.position);
        aim.SetPosition(1, body.position + lockedDirection * length);
        ringAim.enabled = false;
    }

    public void HideAim()
    {
        if (aim != null)
        {
            aim.enabled = false;
        }
    }

    public void ShowRing(float radius)
    {
        BossAgent.ConfigureRing(
            ringAim,
            radius,
            new Color(1f, 0.35f, 0.2f, 0.75f));
        ringAim.enabled = true;
        HideAim();
    }

    public void HideRing()
    {
        if (ringAim != null)
        {
            ringAim.enabled = false;
        }
    }

    public void ArmZone(Vector2 center, float radius, float delay, float damage)
    {
        Acquire()?.ArmZone(center, radius, delay, damage);
    }

    public void FireHazard(Vector2 origin, Vector2 movement, float damage)
    {
        Acquire()?.Fire(origin, movement, damage);
    }

    private BossHazard Acquire()
    {
        for (int i = 0; i < hazards.Length; i++)
        {
            if (hazards[i] != null && !hazards[i].gameObject.activeSelf)
            {
                return hazards[i];
            }
        }

        return null;
    }

    public void TryDamage(float damage)
    {
        // A single volley cannot stack seven contacts into one unavoidable instant kill.
        if (defeated || player == null || player.IsDead ||
            Time.time < nextDamage)
        {
            return;
        }

        nextDamage = Time.time + 0.3f;
        player.TakeDamage(damage);
        CombatFeedback.Instance?.PlayPlayerHit(player.transform.position);
    }

    public void Defeated()
    {
        if (defeated)
        {
            return;
        }

        defeated = true;
        CancelActiveBehavior();
        HideAim();
        HideRing();
        foreach (BossHazard hazard in hazards)
        {
            if (hazard != null)
            {
                hazard.Release();
            }
        }

        encounter.BossDefeated(this);
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    private void OnDisable()
    {
        CancelActiveBehavior();
    }

    private void OnDestroy()
    {
        HideAim();
        HideRing();
        foreach (BossHazard hazard in hazards)
        {
            if (hazard != null)
            {
                Destroy(hazard.gameObject);
            }
        }
    }

    private bool DebugDecisionsHeld
    {
        get
        {
#if UNITY_EDITOR
            return DebugHoldDecisions;
#else
            return false;
#endif
        }
    }

#if UNITY_EDITOR
    public bool DebugStartAction(BossActionType action)
    {
        if (defeated || Health == null || Health.IsDead ||
            player == null || player.IsDead || brain == null)
        {
            return false;
        }

        BossBehavior behavior = brain.GetBehavior(action);
        if (behavior == null || !behavior.IsEligible(BuildSituation()))
        {
            return false;
        }

        CancelActiveBehavior();
        activeBehavior = behavior;
        activeTuning = behavior.Tuning;
        currentAction = action;
        lastAction = action;
        behavior.MarkStarted(Time.time);
        brain.NotifyStarted(action);
        behavior.Begin(this, BuildSituation());
        nextDecisionAt = DebugHoldDecisions ? float.PositiveInfinity : Time.time;
        return true;
    }
#endif

    public static void ConfigureRing(LineRenderer line, float radius, Color color)
    {
        line.sharedMaterial = ArenaPresentation.TrailMaterial;
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = 64;
        line.startWidth = line.endWidth = 0.09f;
        line.startColor = line.endColor = color;
        line.sortingOrder = 9;
        for (int i = 0; i < 64; i++)
        {
            float a = i * Mathf.PI * 2 / 64;
            line.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f));
        }
    }
}
