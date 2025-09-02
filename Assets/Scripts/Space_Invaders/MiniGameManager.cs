using UnityEngine;
using UnityEngine.SceneManagement;

public class MiniGameManager : MonoBehaviour
{
    public static MiniGameManager Instance { get; private set; }

    [Header("Power-up rules")]
    [SerializeField] private int killsToPower = 3;

    [Header("Scene flow")]
    [SerializeField] private string sceneOnWin  = "IsometricHall";
    [SerializeField] private string sceneOnLose = "Tutorial"; // optional

    [Header("Refs")]
    [SerializeField] private PlayerShooter    playerShooter;
    [SerializeField] private MiniPlayerHealth miniHealth;
    [SerializeField] private PowerUpVisual    powerVFX;
    [SerializeField] private MiniGameUI       ui;

    [Header("Events (optional)")]
    [SerializeField] private UnityEngine.Events.UnityEvent onPoweredUp;
    [SerializeField] private UnityEngine.Events.UnityEvent onPlayerFailed;
    [SerializeField] private UnityEngine.Events.UnityEvent onDragonDefeated;

    private int  monsterKills = 0;
    private bool powered = false;
    private bool finished = false;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (!playerShooter) playerShooter = FindFirstObjectByType<PlayerShooter>();
        if (!miniHealth)    miniHealth    = FindFirstObjectByType<MiniPlayerHealth>();
        if (!powerVFX)      powerVFX      = FindFirstObjectByType<PowerUpVisual>();
        if (!ui)            ui            = FindFirstObjectByType<MiniGameUI>();
    }

    void Start()
    {
        monsterKills = 0;
        powered = false;
        finished = false;

        playerShooter?.SetPowered(false);
        powerVFX?.SetPowered(false);
        miniHealth?.ResetLives(3);

        ui?.SetLives(miniHealth ? miniHealth.Lives : 3);
        ui?.SetKills(0, killsToPower);
        ui?.SetPowered(false);
    }

    public void RegisterMonsterKill()
    {
        if (finished) return;

        monsterKills++;
        ui?.SetKills(monsterKills, killsToPower);

        if (!powered && monsterKills >= killsToPower)
        {
            powered = true;
            playerShooter?.SetPowered(true);
            powerVFX?.SetPowered(true);
            ui?.SetPowered(true);
            onPoweredUp?.Invoke();
        }
    }

    public void RegisterDragonDefeat()
    {
        if (finished) return;
        finished = true;
        onDragonDefeated?.Invoke();
        LoadSceneSafe(sceneOnWin);
    }

    public void PlayerFailed()
    {
        if (finished) return;
        finished = true;
        onPlayerFailed?.Invoke();
        if (!string.IsNullOrWhiteSpace(sceneOnLose))
            LoadSceneSafe(sceneOnLose);
    }

    public void NotifyLivesChanged(int newLives)
    {
        ui?.SetLives(newLives);
    }

    private void LoadSceneSafe(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName)) return;

        var fader = FindFirstObjectByType<SceneFader>();
        if (fader != null)
        {
            fader.FadeAndLoad(sceneName);   // now PUBLIC
        }
        else
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }
    }
}