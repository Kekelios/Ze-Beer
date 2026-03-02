using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GamePhase { Menu, BottleSelection, Playing, GameOver }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private BottleData[] availableBottles;
    [SerializeField] private float turnDuration = 15f;
    [SerializeField] private float shakeDuration = 3f;

    public float TurnDuration  => turnDuration;
    public float ShakeDuration => shakeDuration;

    public GamePhase CurrentPhase { get; private set; }
    public BottleModel Bottle     { get; private set; }
    public bool PlayerWon         { get; private set; }

    public event Action<GamePhase> OnPhaseChanged;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ── Phase transitions ────────────────────────────────────────────

    public void GoToBottleSelection()
    {
        SetPhase(GamePhase.BottleSelection);
    }

    /// <summary>Lance la partie avec la bouteille choisie par index.</summary>
    public void StartGame(int bottleIndex)
    {
        Bottle = new BottleModel();
        Bottle.Initialize(availableBottles[bottleIndex]);
        Bottle.OnExploded += HandleExplosion;
        SetPhase(GamePhase.Playing);
    }

    /// <summary>Appelé par TurnManager quand la bouteille explose.</summary>
    private void HandleExplosion()
    {
        bool holderIsPlayer = TurnManager.Instance != null &&
                              TurnManager.Instance.CurrentHolder == 0; // 0 = P1
        PlayerWon = !holderIsPlayer;
        SetPhase(GamePhase.GameOver);
    }

    public void ReturnToMenu()
    {
        SetPhase(GamePhase.Menu);
    }

    private void SetPhase(GamePhase phase)
    {
        CurrentPhase = phase;
        OnPhaseChanged?.Invoke(phase);
    }

    public BottleData[] GetAvailableBottles() => availableBottles;
}
