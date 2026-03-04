using System;
using UnityEngine;

public enum GamePhase { Playing, GameOver }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private BottleData[] availableBottles;
    [SerializeField] private float turnDuration = 15f;
    [SerializeField] private float shakeDuration = 3f;

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

    public void RestartGame()
    {
        StartGame(debugBottleIndex);
    }

    private void HandleExplosion()
    {
        bool holderIsPlayer = TurnManager.Instance != null &&
                              TurnManager.Instance.CurrentHolder == 0;

        PlayerWon = !holderIsPlayer;
        SetPhase(GamePhase.GameOver);
    }

    private void SetPhase(GamePhase phase)
    {
        CurrentPhase = phase;
        OnPhaseChanged?.Invoke(phase);
    }
}