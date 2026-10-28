using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class CombatAITests
{
    private static BossSituation Situation(
        float distance,
        float time = 10f,
        float health = 1f,
        float playerSpeed = 0f,
        float playerEdge = 0f,
        bool final = false)
    {
        return new BossSituation(
            Vector2.zero,
            Vector2.right * distance,
            Vector2.right * playerSpeed,
            health,
            playerEdge,
            0f,
            time,
            final);
    }

    private static BossBehaviorScore Score(
        BossDecision decision,
        BossActionType action)
    {
        for (int i = 0; i < decision.Scores.Count; i++)
        {
            if (decision.Scores[i].Action == action)
            {
                return decision.Scores[i];
            }
        }

        Assert.Fail($"Missing score for {action}.");
        return default;
    }

    [Test]
    public void CoolingDownActionIsUnavailable()
    {
        BossDecisionConfig config = BossDecisionConfig.CreateDefault();
        BossUtilityBrain brain = new BossUtilityBrain(config);
        brain.Evaluate(Situation(7f));
        brain.NotifyStarted(BossActionType.FanVolley);
        brain.GetBehavior(BossActionType.FanVolley).MarkStarted(10f);

        BossDecision decision = brain.Evaluate(Situation(7f, 10.5f));
        BossBehaviorScore fan = Score(decision, BossActionType.FanVolley);

        Assert.That(fan.Available, Is.False);
        Assert.That(fan.CooldownRemaining, Is.GreaterThan(2f));
        Assert.That(decision.Action, Is.Not.EqualTo(BossActionType.FanVolley));
    }

    [Test]
    public void RepeatingTheSameActionReducesItsScore()
    {
        BossDecisionConfig config = BossDecisionConfig.CreateDefault();
        BossUtilityBrain brain = new BossUtilityBrain(config);
        BossDecision baseline = brain.Evaluate(Situation(7f, 10f, final: true));
        float before = Score(baseline, BossActionType.FanVolley).FinalScore;

        brain.NotifyStarted(BossActionType.FanVolley);
        brain.GetBehavior(BossActionType.FanVolley).MarkStarted(10f);
        BossDecision repeated = brain.Evaluate(Situation(7f, 14f, final: true));
        BossBehaviorScore after = Score(repeated, BossActionType.FanVolley);

        Assert.That(after.Repeated, Is.True);
        Assert.That(after.FinalScore, Is.LessThan(before - 1.5f));
        Assert.That(repeated.Action, Is.Not.EqualTo(BossActionType.FanVolley));
    }

    [Test]
    public void DistantPlayerFavorsPursuit()
    {
        BossDecision decision = new BossUtilityBrain(
            BossDecisionConfig.CreateDefault())
            .Evaluate(Situation(16f, 1f));

        Assert.That(decision.Action, Is.EqualTo(BossActionType.Approach));
        Assert.That(
            Score(decision, BossActionType.Approach).FinalScore,
            Is.GreaterThan(Score(decision, BossActionType.FanVolley).FinalScore));
    }

    [Test]
    public void EdgePressureRaisesZoneBombardmentScore()
    {
        BossUtilityBrain brain = new BossUtilityBrain(
            BossDecisionConfig.CreateDefault());
        BossDecision centered = brain.Evaluate(Situation(7f, 1f, playerEdge: 0.05f));
        BossDecision cornered = brain.Evaluate(Situation(7f, 2f, playerEdge: 0.95f));

        Assert.That(
            Score(cornered, BossActionType.ZoneBombardment).FinalScore,
            Is.GreaterThan(
                Score(centered, BossActionType.ZoneBombardment).FinalScore + 0.8f));
    }

    [Test]
    public void RingBarrageUnlocksAtHalfHealth()
    {
        BossUtilityBrain brain = new BossUtilityBrain(
            BossDecisionConfig.CreateDefault());
        BossDecision healthy = brain.Evaluate(Situation(4f, 1f, health: 0.65f));
        BossDecision wounded = brain.Evaluate(Situation(4f, 2f, health: 0.5f));

        Assert.That(
            Score(healthy, BossActionType.RingBarrage).Eligible,
            Is.False);
        Assert.That(
            Score(wounded, BossActionType.RingBarrage).Eligible,
            Is.True);
        Assert.That(
            Score(wounded, BossActionType.RingBarrage).FinalScore,
            Is.GreaterThan(0f));
    }

    [Test]
    public void SpawnBatchStopsAtEnemyCap()
    {
        Assert.That(CombatDirector.ClampBatchToCapacity(8, 10, 3), Is.EqualTo(2));
        Assert.That(CombatDirector.ClampBatchToCapacity(10, 10, 3), Is.Zero);
        Assert.That(CombatDirector.ClampBatchToCapacity(12, 10, 3), Is.Zero);
    }

    [Test]
    public void DirectorStaysInsideConfiguredBounds()
    {
        CombatDirectorConfig config = CombatDirectorConfig.CreateDefault();
        CombatDirectorModel model = new CombatDirectorModel(config);
        CombatDirectorOutput output = model.Output;

        for (int i = 0; i < 20; i++)
        {
            output = model.Step(new CombatDirectorInput(
                i * 0.5f,
                0.5f,
                config.TimeToMaximumProgression,
                1f,
                0f,
                2f,
                1f,
                0,
                output.EnemyCap));
        }

        Assert.That(output.Pressure, Is.EqualTo(1f).Within(0.001f));
        Assert.That(output.SpawnInterval, Is.EqualTo(config.FastestSpawnInterval).Within(0.001f));
        Assert.That(output.EnemyCap, Is.EqualTo(config.MaximumEnemyCap));
        Assert.That(output.BatchCount, Is.EqualTo(config.MaximumBatch));
        Assert.That(output.SpawnInterval, Is.GreaterThanOrEqualTo(config.FastestSpawnInterval));
    }

    [Test]
    public void LowHealthCannotHoldLateRunAtOpeningDifficulty()
    {
        CombatDirectorConfig config = CombatDirectorConfig.CreateDefault();
        CombatDirectorModel model = new CombatDirectorModel(config);
        CombatDirectorOutput output = model.Output;

        for (int i = 0; i < 4; i++)
        {
            output = model.Step(new CombatDirectorInput(
                i * 0.5f,
                0.5f,
                config.TimeToMaximumProgression,
                0.2f,
                0f,
                0f,
                0f,
                0,
                output.EnemyCap));
        }

        Assert.That(
            output.ProgressionPressure,
            Is.EqualTo(config.MaximumProgressionPressure).Within(0.001f));
        Assert.That(
            output.MinimumPressure,
            Is.EqualTo(
                config.MaximumProgressionPressure -
                config.MaximumAdaptiveRelief).Within(0.001f));
        Assert.That(
            output.Pressure,
            Is.GreaterThanOrEqualTo(config.MaximumProgressionPressure - 0.001f));
    }

    [Test]
    public void HeavyDamageAtLowHealthCreatesBoundedRecoveryWindow()
    {
        CombatDirectorConfig config = CombatDirectorConfig.CreateDefault();
        CombatDirectorModel model = new CombatDirectorModel(config);
        CombatDirectorOutput output = model.Step(new CombatDirectorInput(
            1f,
            0.25f,
            1f,
            0.2f,
            0.4f,
            0f,
            0.2f,
            12,
            19));

        Assert.That(output.Recovering, Is.True);
        Assert.That(output.SuppressSpawns, Is.True);
        Assert.That(output.BatchCount, Is.Zero);
        Assert.That(output.EnemyCap, Is.EqualTo(config.MinimumEnemyCap));

        output = model.Step(new CombatDirectorInput(
            1.25f + config.RecoveryDuration,
            0.25f,
            1.25f + config.RecoveryDuration,
            0.2f,
            0.4f,
            0f,
            0.2f,
            4,
            output.EnemyCap));

        Assert.That(output.Recovering, Is.False);
        Assert.That(output.RecoveryRemaining, Is.Zero);
        Assert.That(output.BatchCount, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public void RecoveryCannotRetriggerImmediately()
    {
        CombatDirectorConfig config = CombatDirectorConfig.CreateDefault();
        CombatDirectorModel model = new CombatDirectorModel(config);
        model.Step(new CombatDirectorInput(
            0f, 0.1f, 0f, 0.2f, 0.5f, 0f, 0f, 10, 14));
        model.Step(new CombatDirectorInput(
            config.RecoveryDuration + 0.1f,
            0.1f,
            config.RecoveryDuration + 0.1f,
            0.2f,
            0.5f,
            0f,
            0f,
            4,
            config.MinimumEnemyCap));

        CombatDirectorOutput output = model.Step(new CombatDirectorInput(
            config.RecoveryDuration + 0.2f,
            0.1f,
            config.RecoveryDuration + 0.2f,
            0.2f,
            0.5f,
            0f,
            0f,
            4,
            config.MinimumEnemyCap));

        Assert.That(output.Recovering, Is.False);
        Assert.That(output.RecoveryArmed, Is.False);
    }

    [Test]
    public void RecoveryRequiresHealthToRearmBeforeRetriggering()
    {
        CombatDirectorConfig config = CombatDirectorConfig.CreateDefault();
        CombatDirectorModel model = new CombatDirectorModel(config);
        model.Step(new CombatDirectorInput(
            0f, 0.1f, 0f, 0.2f, 0.5f, 0f, 0f, 4, 10));

        float afterCooldown =
            config.RecoveryDuration + config.RecoveryCooldown + 0.2f;
        CombatDirectorOutput stillLow = model.Step(new CombatDirectorInput(
            afterCooldown,
            0.1f,
            afterCooldown,
            0.2f,
            0.5f,
            0f,
            0f,
            4,
            10));

        Assert.That(stillLow.Recovering, Is.False);
        Assert.That(stillLow.RecoveryArmed, Is.False);

        CombatDirectorOutput rearmed = model.Step(new CombatDirectorInput(
            afterCooldown + 0.1f,
            0.1f,
            afterCooldown + 0.1f,
            config.RecoveryRearmHealthThreshold + 0.05f,
            0f,
            0f,
            0f,
            4,
            10));
        Assert.That(rearmed.RecoveryArmed, Is.True);

        CombatDirectorOutput retriggered = model.Step(new CombatDirectorInput(
            afterCooldown + 0.2f,
            0.1f,
            afterCooldown + 0.2f,
            0.2f,
            0.5f,
            0f,
            0f,
            4,
            10));

        Assert.That(retriggered.Recovering, Is.True);
        Assert.That(retriggered.RecoveryArmed, Is.False);
    }

    [UnityTest]
    public IEnumerator LiveDirectorEntersRecoveryAfterHeavyDamage()
    {
        SceneManager.LoadScene("Main");
        yield return null;
        yield return null;
        yield return null;

        GameManager game = Object.FindFirstObjectByType<GameManager>();
        game.StartRun();
        yield return null;
        yield return null;
        GameObject.Find("UpgradeButton1").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
        yield return null;
        yield return null;

        EnemySpawner spawner = Object.FindFirstObjectByType<EnemySpawner>();
        Assert.That(spawner.Director, Is.Not.Null);
        spawner.enabled = false;
        Health health = Object.FindFirstObjectByType<PlayerShooter>().GetComponent<Health>();
        health.TakeDamage(80f);
        yield return null;
        yield return null;

        Assert.That(spawner.Director.SpawnsSuppressed, Is.True);
        Assert.That(spawner.Director.CurrentBatch, Is.Zero);
        Assert.That(spawner.Director.RecoveryRemaining, Is.GreaterThan(0f));
    }
}
