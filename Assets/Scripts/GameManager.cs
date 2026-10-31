using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("Gameplay")]
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerShooter shooter;
    [SerializeField] private EnemySpawner spawner;
    [SerializeField] private Health playerHealth;
    [SerializeField] private PlayerProgress progress;
    [SerializeField] private UpgradeController upgradeController;

    [Header("HUD")]
    [SerializeField] private HudController hud;
    [SerializeField] private GameObject startPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text bestText;
    [SerializeField] private TMP_Text finalStatsText;

    [Header("Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button pauseRestartButton;
    [SerializeField] private Button restartButton;

    private bool hasStarted;
    private bool isChoosingStartingWeapon;
    private bool isPaused;
    private bool isGameOver;
    private float elapsedTime;
    private int killCount;
    public bool IsRunning => hasStarted && !isGameOver;
    public bool HasWon { get; private set; }

    private void Awake()
    {
        if (Application.isMobilePlatform)
        {
            Application.targetFrameRate = 60;
        }

        Time.timeScale = 0f;
        MobileControlsOverlay.SetGameplayActive(false);
        movement.enabled = false;
        shooter.enabled = false;
        spawner.enabled = false;

        startPanel.SetActive(true);
        pausePanel.SetActive(false);
        gameOverPanel.SetActive(false);
        pauseButton.interactable = false;

        startButton.onClick.AddListener(StartRun);
        pauseButton.onClick.AddListener(TogglePause);
        resumeButton.onClick.AddListener(ResumeRun);
        pauseRestartButton.onClick.AddListener(RestartRun);
        restartButton.onClick.AddListener(RestartRun);
        gameObject.AddComponent<BossEncounter>().Initialize(this, shooter, playerHealth, progress, spawner, upgradeController);
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        gameObject.AddComponent<CombatDebugOverlay>();
#endif
    }

    private void Start()
    {
        hud.Initialize(playerHealth, progress);
        playerHealth.Died += HandlePlayerDied;
        EnemyController.DiedGlobally += HandleEnemyDied;
        progress.LevelChanged += HandleLevelChanged;

        float bestTime = PlayerPrefs.GetFloat("BestTime", 0f);
        int bestKills = PlayerPrefs.GetInt("BestKills", 0);
        bestText.text =
            $"BEST  {FormatTime(bestTime)}  |  {bestKills} KILLS";
        hud.SetState("READY");
        SelectButton(startButton);
        PortfolioSettingsMenu.Place(restartButton.transform,new Vector2(.08f,.08f),new Vector2(.48f,.22f));
        var quit=PortfolioSettingsMenu.MakeButton(restartButton.transform.parent,"Quit Result",
            Application.platform == RuntimePlatform.WebGLPlayer ? "RETURN TO TITLE" : "QUIT",QuitRun);
        PortfolioSettingsMenu.Place(quit.transform,new Vector2(.52f,.08f),new Vector2(.92f,.22f));
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.Died -= HandlePlayerDied;
        }

        EnemyController.DiedGlobally -= HandleEnemyDied;
        if (progress != null)
        {
            progress.LevelChanged -= HandleLevelChanged;
        }

        startButton?.onClick.RemoveListener(StartRun);
        pauseButton?.onClick.RemoveListener(TogglePause);
        resumeButton?.onClick.RemoveListener(ResumeRun);
        pauseRestartButton?.onClick.RemoveListener(RestartRun);
        restartButton?.onClick.RemoveListener(RestartRun);
    }

    private void Update()
    {
        if (PortfolioSettings.IsOpen) return;
        Keyboard keyboard = Keyboard.current;

        if (!hasStarted && !isChoosingStartingWeapon && keyboard != null &&
            keyboard.enterKey.wasPressedThisFrame)
        {
            StartRun();
        }

        if (hasStarted && !isGameOver && !upgradeController.IsOpen &&
            keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }

        if (isGameOver && keyboard != null &&
            keyboard.rKey.wasPressedThisFrame)
        {
            RestartRun();
        }

        if (hasStarted && !isGameOver && !isPaused &&
            !upgradeController.IsOpen)
        {
            elapsedTime += Time.unscaledDeltaTime;
            hud.SetTimer(elapsedTime);
        }
    }

    public void StartRun()
    {
        if (PortfolioSettings.IsOpen) return;
        if (hasStarted || isChoosingStartingWeapon)
        {
            return;
        }

        isChoosingStartingWeapon = true;
        startPanel.SetActive(false);
        hud.SetState("CHOOSE WEAPON");
        ClearSelection();

        if (upgradeController.ShowStartingWeaponChoices(BeginRun))
        {
            return;
        }

        isChoosingStartingWeapon = false;
        startPanel.SetActive(true);
        hud.SetState("READY");
        SelectButton(startButton);
    }

    private void BeginRun()
    {
        if (hasStarted || !isChoosingStartingWeapon)
        {
            return;
        }

        isChoosingStartingWeapon = false;
        hasStarted = true;
        Time.timeScale = 1f;
        movement.enabled = true;
        shooter.enabled = true;
        spawner.enabled = true;
        pauseButton.interactable = true;
        hud.SetState(string.Empty);
        MobileControlsOverlay.SetGameplayActive(true);
        ClearSelection();
    }

    public void TogglePause()
    {
        if (!hasStarted || isGameOver || upgradeController.IsOpen)
        {
            return;
        }

        if (isPaused)
        {
            ResumeRun();
            return;
        }

        isPaused = true;
        MobileControlsOverlay.SetGameplayActive(false);
        pausePanel.transform.SetAsLastSibling();
        pausePanel.SetActive(true);
        hud.SetState("PAUSED");
        Time.timeScale = 0f;
        SelectButton(resumeButton);
    }

    public void ResumeRun()
    {
        if (!isPaused || isGameOver)
        {
            return;
        }

        isPaused = false;
        pausePanel.SetActive(false);
        hud.SetState(string.Empty);
        Time.timeScale = 1f;
        MobileControlsOverlay.SetGameplayActive(true);
        ClearSelection();
    }

    public void RestartRun()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void HandleLevelChanged(int level)
    {
        if (level <= 1)
        {
            return;
        }

        CombatFeedback.Instance?.PlayLevelUp(
            progress.transform.position);
    }

    private void HandleEnemyDied()
    {
        if (!hasStarted || isGameOver)
        {
            return;
        }

        killCount++;
        hud.SetKills(killCount);
    }

    private void HandlePlayerDied()
    {
        if (isGameOver)
        {
            return;
        }

        isGameOver = true;
        isPaused = false;
        MobileControlsOverlay.SetGameplayActive(false);
        movement.enabled = false;
        shooter.enabled = false;
        spawner.enabled = false;
        pauseButton.interactable = false;
        pausePanel.SetActive(false);
        Time.timeScale = 0f;

        float bestTime = Mathf.Max(
            PlayerPrefs.GetFloat("BestTime", 0f),
            elapsedTime);
        int bestKills = Mathf.Max(
            PlayerPrefs.GetInt("BestKills", 0),
            killCount);
        PlayerPrefs.SetFloat("BestTime", bestTime);
        PlayerPrefs.SetInt("BestKills", bestKills);
        PlayerPrefs.Save();

        finalStatsText.text =
            $"TIME  {FormatTime(elapsedTime)}\n" +
            $"KILLS  {killCount}\n\n" +
            $"BEST  {FormatTime(bestTime)}  |  {bestKills} KILLS";
        gameOverPanel.transform.SetAsLastSibling();
        gameOverPanel.SetActive(true);
        foreach (var label in gameOverPanel.GetComponentsInChildren<TMP_Text>(true))
            if (label.name == "TitleText")
            {
                label.text = HasWon ? "VICTORY" : "RUN OVER";
                label.color = HasWon ? new Color(.4f,1f,.7f) : new Color(1f,.35f,.4f);
            }
        hud.SetState(HasWon ? "FINAL BOSS DEFEATED" : "RUN OVER");
        SelectButton(restartButton);
    }

    public void WinRun()
    {
        if (isGameOver || playerHealth.IsDead) return;
        HasWon = true;
        HandlePlayerDied();
    }

    public void QuitRun()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#elif UNITY_WEBGL
        RestartRun();
#else
        Application.Quit();
#endif
    }

    private static void SelectButton(Button button)
    {
        if (EventSystem.current == null || button == null ||
            !button.gameObject.activeInHierarchy)
        {
            return;
        }

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(button.gameObject);
    }

    private static void ClearSelection()
    {
        EventSystem.current?.SetSelectedGameObject(null);
    }

    private static string FormatTime(float seconds)
    {
        int totalSeconds = Mathf.FloorToInt(seconds);
        return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
    }
}
