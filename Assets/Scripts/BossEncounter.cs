using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum BossStage { Collecting, MiniIntro, MiniFight, Rewards, FinalIntro, FinalFight, Victory, Defeat }

public sealed class BossEncounter : MonoBehaviour
{
    private GameManager game;
    private PlayerShooter shooter;
    private Health player;
    private PlayerProgress progress;
    private EnemySpawner spawner;
    private UpgradeController upgrades;
    private ArenaBounds bounds;
    private Text status, remaining;
    private Image healthFill;
    private GameObject bar, rewardPanel;
    private readonly Button[] rewardButtons = new Button[6];
    private readonly bool[] selected = new bool[6];
    private int selectedCount;
    private float powerAtSix, regeneration;
    private GameObject arrival;
    public BossStage Stage { get; private set; }
    public BossAgent ActiveBoss { get; private set; }
    public int RewardsSelected => selectedCount;

    public void Initialize(GameManager manager,PlayerShooter weapons,Health health,PlayerProgress levels,EnemySpawner enemies,UpgradeController choices)
    { game=manager;shooter=weapons;player=health;progress=levels;spawner=enemies;upgrades=choices; }
    private void Start()
    {
        bounds=FindFirstObjectByType<ArenaBounds>();
        BuildUi();
    }
    private void Update()
    {
        if (player.IsDead && Stage != BossStage.Defeat)
        {
            Stage=BossStage.Defeat;StopAllCoroutines();ClearHazards();rewardPanel.SetActive(false);bar.SetActive(false);upgrades.ExternalChoiceOpen=false;
            if(arrival!=null)Destroy(arrival);
        }
        if (!game.IsRunning || Time.timeScale==0) return;
        if (regeneration>0) player.Heal(regeneration*Time.deltaTime);
        if(Stage==BossStage.Collecting && shooter.EquippedCount==6 && !upgrades.IsOpen)
        {
            powerAtSix=shooter.EstimatedDamagePerSecond;
            StartCoroutine(Summon(false));
        }
        if (ActiveBoss != null && !ActiveBoss.Health.IsDead)
        {
            var health=ActiveBoss.Health;
            healthFill.rectTransform.anchorMax=new Vector2(health.Current/health.Max,1);
            status.text=$"{(ActiveBoss.IsFinal ? "OVERLORD" : "WARDEN")}  {Mathf.CeilToInt(health.Current)} / {Mathf.CeilToInt(health.Max)}";
        }
    }
    private IEnumerator Summon(bool final)
    {
        Stage=final ? BossStage.FinalIntro : BossStage.MiniIntro;
        if(!final)
        {
            spawner.enabled=false;
            ClearRegularWave();
        }
        bar.SetActive(true); status.text=final ? "FINAL BATTLE" : "BOSS APPROACHING";
        healthFill.rectTransform.anchorMax=Vector2.one;
        var position=FindSpawnPoint();
        var warning=new GameObject("Boss Arrival");arrival=warning;warning.transform.SetParent(transform);warning.transform.position=position;
        var ring=warning.AddComponent<LineRenderer>();BossAgent.ConfigureRing(ring,2f,new Color(1f,.56f,.12f));
        yield return new WaitForSeconds(2f);
        Destroy(warning);
        arrival=null;
        if(!game.IsRunning || player.IsDead) yield break;
        var root=new GameObject(final ? "Overlord" : "Warden");root.SetActive(false);root.tag="Enemy";
        root.transform.position=position;root.transform.localScale=Vector3.one*(final?2.8f:2.2f);
        var sprite=root.AddComponent<SpriteRenderer>();sprite.sprite=Resources.Load<Sprite>(final ? "Polish/Overlord" : "Polish/Warden");sprite.sortingOrder=6;
        var body=root.AddComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Kinematic;body.gravityScale=0;
        var collider=root.AddComponent<CircleCollider2D>();collider.isTrigger=true;collider.radius=.43f;
        root.AddComponent<EnemyController>();
        ActiveBoss=root.AddComponent<BossAgent>();
        root.SetActive(true);
        ActiveBoss.Initialize(this,player,bounds,final,Mathf.Clamp(powerAtSix*(final?50:20),final?12000:4000,final?45000:16000));
        Stage=final ? BossStage.FinalFight : BossStage.MiniFight;
    }
    private Vector2 FindSpawnPoint()
    {
        var origin=(Vector2)player.transform.position;var best=origin;var farthest=0f;
        for(var i=0;i<16;i++)
        {
            var angle=i*Mathf.PI/8;var candidate=bounds.ClampPoint(origin+new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*8,2);
            var distance=(candidate-origin).sqrMagnitude;
            if(distance>farthest){farthest=distance;best=candidate;}
        }
        return best;
    }
    public void BossDefeated(BossAgent boss)
    {
        if(boss!=ActiveBoss || !game.IsRunning || player.IsDead) return;
        var final=boss.IsFinal;
        ActiveBoss=null;ClearHazards();bar.SetActive(false);
        if(final)
        {
            Stage=BossStage.Victory;game.WinRun();return;
        }
        Stage=BossStage.Rewards;
        spawner.enabled=false;
        // Clear the previous wave, including pending spawns, before the final duel.
        ClearRegularWave();
        upgrades.ExternalChoiceOpen=true;
        progress.GrantBossLevels();
        rewardPanel.SetActive(true);Time.timeScale=0;MobileControlsOverlay.SetGameplayActive(false);
        remaining.text="REWARDS REMAINING  2";
    }
    public bool ChooseReward(int index)
    {
        if(Stage!=BossStage.Rewards || index<0 || index>=6 || selected[index] || selectedCount>=2) return false;
        selected[index]=true;selectedCount++;rewardButtons[index].interactable=false;
        rewardButtons[index].GetComponent<Image>().color=new Color(.15f,.31f,.25f);
        rewardButtons[index].transform.Find("Name").GetComponent<Text>().text="SELECTED";
        switch(index)
        {
            case 0:shooter.ApplyGoldenDamage();break;
            case 1:shooter.ApplyGoldenSpeed();break;
            case 2:player.AddArmor(.35f);break;
            case 3:player.AddMaxHealth(60);break;
            case 4:regeneration=3;break;
            case 5:shooter.ApplyGoldenRange();break;
        }
        remaining.text="REWARDS REMAINING  "+(2-selectedCount);
        if(selectedCount==2)
        {
            rewardPanel.SetActive(false);upgrades.ExternalChoiceOpen=false;Time.timeScale=1;MobileControlsOverlay.SetGameplayActive(true);
            StartCoroutine(Summon(true));
        }
        return true;
    }
    private void BuildUi()
    {
        var canvas=new GameObject("Boss Canvas",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvas.transform.SetParent(transform);
        canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;canvas.GetComponent<Canvas>().sortingOrder=100;
        var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
        var safe=new GameObject("Boss Safe Area",typeof(RectTransform),typeof(SafeSettingsArea));safe.transform.SetParent(canvas.transform,false);PortfolioSettingsMenu.Place(safe.transform,Vector2.zero,Vector2.one);
        bar=PortfolioSettingsMenu.Box(safe.transform,"Boss Health",new Color(.06f,.035f,.05f,.95f));
        PortfolioSettingsMenu.Place(bar.transform,new Vector2(.22f,.78f),new Vector2(.78f,.87f));
        status=PortfolioSettingsMenu.MakeText(bar.transform,"Boss Name","",23);status.alignment=TextAnchor.MiddleCenter;
        PortfolioSettingsMenu.Place(status.transform,new Vector2(.02f,.30f),new Vector2(.98f,1));
        var track=PortfolioSettingsMenu.Box(bar.transform,"Track",new Color(.23f,.15f,.15f));PortfolioSettingsMenu.Place(track.transform,new Vector2(.03f,.1f),new Vector2(.97f,.24f));
        healthFill=PortfolioSettingsMenu.Box(track.transform,"Fill",new Color(1f,.5f,.18f)).GetComponent<Image>();bar.SetActive(false);
        rewardPanel=PortfolioSettingsMenu.Box(safe.transform,"Golden Rewards",new Color(.055f,.045f,.025f,1f));
        var title=PortfolioSettingsMenu.MakeText(rewardPanel.transform,"Title","GOLDEN REWARDS / CHOOSE TWO",36);title.color=new Color(1f,.81f,.3f);
        PortfolioSettingsMenu.Place(title.transform,new Vector2(.06f,.85f),new Vector2(.94f,.95f));
        remaining=PortfolioSettingsMenu.MakeText(rewardPanel.transform,"Remaining","",24);PortfolioSettingsMenu.Place(remaining.transform,new Vector2(.06f,.75f),new Vector2(.94f,.83f));
        var names=new[]{"POWER +40%","HASTE +30%","ARMOR +35%","VITALITY +60","REGENERATION","RANGE +30%"};
        var descriptions=new[]{"Multiply all weapon damage by 1.4.","Multiply attack frequency by 1.3.","Reduce all incoming damage by 35%.","Gain 60 maximum health and heal 60.","Recover 3 health per second while alive.","Multiply all weapon ranges by 1.3."};
        for(var i=0;i<6;i++)
        {
            var index=i;var button=PortfolioSettingsMenu.MakeButton(rewardPanel.transform,"Reward "+i,"",()=>ChooseReward(index));
            var x=.06f+i%3*.30f;var y=.43f-i/3*.31f;PortfolioSettingsMenu.Place(button.transform,new Vector2(x,y),new Vector2(x+.28f,y+.27f));
            button.GetComponent<Image>().color=new Color(.26f,.18f,.065f);
            var outline=button.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.95f,.68f,.18f);outline.effectDistance=Vector2.one*2;
            var name=PortfolioSettingsMenu.MakeText(button.transform,"Name",names[i],27);name.color=new Color(1f,.85f,.4f);PortfolioSettingsMenu.Place(name.transform,new Vector2(.07f,.55f),new Vector2(.93f,.92f));
            var description=PortfolioSettingsMenu.MakeText(button.transform,"Description",descriptions[i],22);PortfolioSettingsMenu.Place(description.transform,new Vector2(.07f,.09f),new Vector2(.93f,.56f));rewardButtons[i]=button;
        }
        rewardPanel.SetActive(false);
    }
    private static void ClearHazards()
    {
        foreach(var hazard in FindObjectsByType<BossHazard>(FindObjectsSortMode.None))hazard.Release();
    }
    private static void ClearRegularWave()
    {
        foreach(var warning in FindObjectsByType<EnemySpawnWarning>(FindObjectsSortMode.None))
            warning.GetComponent<PoolMember>().Release();
        foreach(var enemy in FindObjectsByType<EnemyController>(FindObjectsSortMode.None))
            if(enemy.GetComponent<BossAgent>()==null)
                enemy.GetComponent<PoolMember>().Release();
    }
}
