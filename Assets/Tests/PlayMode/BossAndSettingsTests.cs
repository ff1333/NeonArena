using System.Collections;
using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class BossAndSettingsTests
{
    private GameManager game;
    private PlayerShooter shooter;
    private BossEncounter encounter;
    private Health health;
    private bool chinese, sound;
    private float master, music, effects;
    [UnitySetUp]
    public IEnumerator Setup()
    {
        chinese=PortfolioSettings.Chinese;sound=PortfolioSettings.SoundEnabled;
        master=PortfolioSettings.Master;music=PortfolioSettings.Music;effects=PortfolioSettings.Effects;
        SceneManager.LoadScene("Main");yield return null;yield return null;yield return null;
        game=Object.FindFirstObjectByType<GameManager>();shooter=Object.FindFirstObjectByType<PlayerShooter>();
        encounter=game.GetComponent<BossEncounter>();health=shooter.GetComponent<Health>();
    }
    private IEnumerator Begin()
    {
        game.StartRun();yield return null;yield return null;
        GameObject.Find("UpgradeButton1").GetComponent<Button>().onClick.Invoke();yield return null;
        Object.FindFirstObjectByType<EnemySpawner>().enabled=false;
        shooter.enabled=false;shooter.GetComponent<PlayerMovement>().enabled=false;
        foreach(var warning in Object.FindObjectsByType<EnemySpawnWarning>(FindObjectsSortMode.None))warning.GetComponent<PoolMember>().Release();
    }
    private IEnumerator MiniBoss()
    {
        yield return Begin();
        var weapon=Resources.FindObjectsOfTypeAll<WeaponDefinition>().First();
        while(shooter.EquippedCount<5)Assert.That(shooter.EquipWeapon(weapon),Is.True);
        yield return null;Assert.That(encounter.Stage,Is.EqualTo(BossStage.Collecting));
        shooter.EquipWeapon(weapon);yield return null;
        Assert.That(encounter.Stage,Is.EqualTo(BossStage.MiniIntro));
        yield return new WaitForSeconds(2.2f);
        Assert.That(encounter.Stage,Is.EqualTo(BossStage.MiniFight));
        Assert.That(Vector2.Distance(encounter.ActiveBoss.transform.position,shooter.transform.position),Is.GreaterThan(5));
    }
    [UnityTest]
    public IEnumerator SixWeaponsRewardsAndFinalBossCompleteVictory()
    {
        yield return MiniBoss();
        var progress=shooter.GetComponent<PlayerProgress>();var level=progress.Level;
        var power=shooter.EstimatedDamagePerSecond;
        encounter.ActiveBoss.Health.TakeDamage(1000000);
        Assert.That(encounter.Stage,Is.EqualTo(BossStage.Rewards));
        Assert.That(progress.Level,Is.EqualTo(level+2));Assert.That(Time.timeScale,Is.Zero);
        Assert.That(GameObject.Find("Golden Rewards").GetComponentsInChildren<Button>().Length,Is.EqualTo(6));
        Assert.That(encounter.ChooseReward(0),Is.True);Assert.That(encounter.ChooseReward(0),Is.False);
        Assert.That(shooter.EstimatedDamagePerSecond,Is.EqualTo(power*1.4f).Within(.01));
        Assert.That(encounter.Stage,Is.EqualTo(BossStage.Rewards));
        Assert.That(encounter.ChooseReward(2),Is.True);Assert.That(health.DamageReduction,Is.EqualTo(.35f));
        Assert.That(encounter.ChooseReward(1),Is.False);
        Assert.That(encounter.Stage,Is.EqualTo(BossStage.FinalIntro));
        yield return new WaitForSeconds(2.2f);
        Assert.That(encounter.ActiveBoss.IsFinal,Is.True);
        encounter.ActiveBoss.Health.TakeDamage(1000000);
        Assert.That(encounter.Stage,Is.EqualTo(BossStage.Victory));Assert.That(game.HasWon,Is.True);Assert.That(Time.timeScale,Is.Zero);
        Assert.That(GameObject.Find("Quit Result"),Is.Not.Null);
    }
    [UnityTest]
    public IEnumerator TelegraphAllowsEscapeButPunishesStandingStill()
    {
        yield return MiniBoss();encounter.ActiveBoss.enabled=false;
        var hazard=Object.FindObjectsByType<BossHazard>(FindObjectsInactive.Include,FindObjectsSortMode.None).First();
        shooter.transform.position=Vector3.zero;
        hazard.ArmZone(Vector2.zero,2.5f,.6f,30);
        yield return new WaitForSeconds(.2f);Assert.That(health.Current,Is.EqualTo(100));
        shooter.transform.position=Vector3.right*5;
        yield return new WaitForSeconds(.5f);Assert.That(health.Current,Is.EqualTo(100));
        hazard.ArmZone(shooter.transform.position,2.5f,.3f,30);
        yield return new WaitForSeconds(.4f);Assert.That(health.Current,Is.EqualTo(70));
    }
    [UnityTest]
    public IEnumerator DeathDuringArrivalDoesNotSpawnBoss()
    {
        yield return Begin();var weapon=Resources.FindObjectsOfTypeAll<WeaponDefinition>().First();
        while(shooter.EquippedCount<6)shooter.EquipWeapon(weapon);
        yield return null;health.TakeDamage(1000);yield return null;
        Assert.That(encounter.Stage,Is.EqualTo(BossStage.Defeat));
        Time.timeScale=1;yield return new WaitForSeconds(2.2f);
        Assert.That(encounter.ActiveBoss,Is.Null);Assert.That(game.HasWon,Is.False);
        Assert.That(GameObject.Find("Boss Arrival"),Is.Null);
    }
    [UnityTest]
    public IEnumerator VitalityAndRegenerationWorkDuringFinalArrival()
    {
        yield return MiniBoss();encounter.ActiveBoss.Health.TakeDamage(1000000);
        encounter.ChooseReward(3);Assert.That(health.Max,Is.EqualTo(160));
        encounter.ChooseReward(4);health.TakeDamage(50);
        yield return new WaitForSeconds(1.1f);
        Assert.That(health.Current,Is.InRange(113f,114f));
    }
    [UnityTest]
    public IEnumerator StationaryPlayerDiesInFinalFightWithoutDodging()
    {
        yield return MiniBoss();encounter.ActiveBoss.Health.TakeDamage(1000000);
        encounter.ChooseReward(0);encounter.ChooseReward(1);
        yield return new WaitForSeconds(2.2f);
        var deadline=Time.time+25;
        while(!health.IsDead && Time.time<deadline)yield return null;
        Assert.That(health.IsDead,Is.True,"Final fight must threaten an idle player.");
        Assert.That(game.HasWon,Is.False);
    }
    [UnityTest]
    public IEnumerator EveryBossBehaviorExecutesAndCompletes()
    {
        yield return MiniBoss();
        var boss=encounter.ActiveBoss;
        boss.DebugHoldDecisions=true;
        health.AddMaxHealth(1000);
        var actions=new[]
        {
            BossActionType.Approach,BossActionType.PredictiveDash,
            BossActionType.FanVolley,BossActionType.ZoneBombardment,
            BossActionType.Reposition
        };
        foreach(var action in actions)
        {
            Assert.That(boss.DebugStartAction(action),Is.True,$"{action} did not start.");
            Assert.That(boss.CurrentAction,Is.EqualTo(action));
            yield return new WaitForSeconds(1.5f);
            Assert.That(boss.LastCompletedAction,Is.EqualTo(action),$"{action} did not complete.");
        }
        boss.Health.TakeDamage(boss.Health.Max*.55f);
        Assert.That(boss.DebugStartAction(BossActionType.RingBarrage),Is.True);
        yield return new WaitForSeconds(1.5f);
        Assert.That(boss.LastCompletedAction,Is.EqualTo(BossActionType.RingBarrage));
    }
    [UnityTest]
    public IEnumerator BossDeathStopsDecisionAndReleasesHazards()
    {
        yield return MiniBoss();
        var boss=encounter.ActiveBoss;
        boss.ArmZone(Vector2.zero,2f,.5f,10);
        yield return null;
        boss.Health.TakeDamage(1000000);
        yield return null;
        Assert.That(encounter.ActiveBoss,Is.Null);
        Assert.That(encounter.Stage,Is.EqualTo(BossStage.Rewards));
        foreach(var hazard in Object.FindObjectsByType<BossHazard>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            Assert.That(hazard.gameObject.activeSelf,Is.False);
    }
    [UnityTest]
    public IEnumerator ChineseEnglishSettingsAndVolumePersistAcrossReload()
    {
        PortfolioSettings.SetLanguage(true);yield return null;
        var title=GameObject.Find("StartPanel").GetComponentsInChildren<TMP_Text>().First(t=>t.name=="TitleText");
        title.GetComponent<LocalizedLabel>().Refresh();Assert.That(title.text,Is.EqualTo("霓虹竞技场"));
        PortfolioSettingsMenu.Instance.Open();game.StartRun();Assert.That(game.IsRunning,Is.False);
        PortfolioSettings.SetLanguage(false);title.GetComponent<LocalizedLabel>().Refresh();Assert.That(title.text,Is.EqualTo("NEON ARENA"));
        PortfolioSettings.SetVolume("Master",.42f);PortfolioSettings.SetVolume("Music",.19f);PortfolioSettings.SetVolume("Effects",.23f);
        PortfolioSettings.SetSound(false);Assert.That(AudioListener.volume,Is.Zero);
        PortfolioSettingsMenu.Instance.Close();Assert.That(Time.timeScale,Is.Zero);
        SceneManager.LoadScene("Main");yield return null;yield return null;yield return null;
        Assert.That(PortfolioSettings.Chinese,Is.False);Assert.That(PortfolioSettings.Master,Is.EqualTo(.42f));
        Assert.That(PortfolioSettings.Music,Is.EqualTo(.19f));Assert.That(PortfolioSettings.Effects,Is.EqualTo(.23f));
        Assert.That(AudioListener.volume,Is.Zero);
        PortfolioSettings.SetSound(true);Assert.That(AudioListener.volume,Is.EqualTo(.42f));
        var source=Object.FindFirstObjectByType<CombatFeedback>().GetComponent<AudioSource>();
        Assert.That(source.volume,Is.EqualTo(.65f*.23f).Within(.001));
    }
    [UnityTearDown]
    public IEnumerator TearDown()
    {
        PortfolioSettings.SetLanguage(chinese);PortfolioSettings.SetSound(sound);PortfolioSettings.SetVolume("Master",master);PortfolioSettings.SetVolume("Music",music);PortfolioSettings.SetVolume("Effects",effects);PortfolioSettings.Save();
        Time.timeScale=1;yield return null;
    }
}
