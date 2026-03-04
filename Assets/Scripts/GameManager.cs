using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GamePhase { Playing, GameOver }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private BottleData[] availableBottles;
    [SerializeField] private float turnDuration = 15f;
    [SerializeField] private float shakeDuration = 3f;

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Debug – Démarrage rapide")]
    [SerializeField] private int debugBottleIndex = 0;

    public float TurnDuration => turnDuration;
    public float ShakeDuration => shakeDuration;

    public GamePhase CurrentPhase { get; private set; }
    public BottleModel Bottle { get; private set; }
    public bool PlayerWon { get; private set; }

    public event Action<GamePhase> OnPhaseChanged;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        StartGame(debugBottleIndex);
    }

    /// <summary>Permet au menu de définir la bouteille avant de charger la scène.</summary>
    public void SetBottleIndex(int index)
    {
        debugBottleIndex = Mathf.Clamp(index, 0, availableBottles != null ? availableBottles.Length - 1 : 0);
    }

    /// <summary>Démarre une partie avec la bouteille à l'index donné.</summary>
    public void StartGame(int bottleIndex)
    {
        bottleIndex = Mathf.Clamp(bottleIndex, 0, availableBottles.Length - 1);

        var data = availableBottles[bottleIndex];
        if (data == null)
        {
            Debug.LogError("[GameManager] BottleData null !");
            return;
        }

        Bottle = new BottleModel();
        Bottle.Initialize(data);
        Bottle.OnExploded += HandleExplosion;

        Debug.Log($"[GameManager] Starting with bottle: {data.bottleName} ({data.bottleType})");

        SetPhase(GamePhase.Playing);
    }

    /// <summary>Retourne au menu principal.</summary>
    public void GoToMainMenu()
    {
        SoundManager.Instance?.StopAllSFX();
        SoundManager.Instance?.PlayMusicIngame();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void HandleExplosion()
    {
        bool holderIsPlayer = TurnManager.Instance != null &&
                              TurnManager.Instance.CurrentHolder == 0;

        PlayerWon = !holderIsPlayer;

        SoundManager.Instance?.PlayBottleExplosion(Bottle.Data.bottleType);

        if (PlayerWon) SoundManager.Instance?.PlayVictorySequence();
        else SoundManager.Instance?.PlayGameOverSequence();

        SetPhase(GamePhase.GameOver);
    }

    private void SetPhase(GamePhase phase)
    {
        CurrentPhase = phase;
        OnPhaseChanged?.Invoke(phase);
    }
}
