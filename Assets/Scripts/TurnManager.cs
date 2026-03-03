using System;
using System.Collections;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    private const int PlayerCount      = 4;
    private const int HumanPlayerIndex = 0;

    [Header("Roulette")]
    [SerializeField] private int   rouletteMinFullLoops = 2;
    [SerializeField] private float rouletteIntervalMin  = 0.08f;
    [SerializeField] private float rouletteIntervalMax  = 0.45f;

    // ── State ─────────────────────────────────────────────────────────

    public int   CurrentHolder    { get; private set; }
    public int   ShakesThisTurn   { get; private set; }
    public float TimeLeft         => Mathf.Max(0f, _turnEndTime - Time.time);
    public bool  InputBlocked     { get; private set; }

    // ── Events ────────────────────────────────────────────────────────

    public event Action<int> OnTurnStarted;    // slot index du joueur actif (0–3)
    public event Action<int> OnRouletteUpdate; // slot index surligné pendant la roulette
    public event Action      OnShakePerformed;
    public event Action      OnTurnEnded;

    // ── Internals ─────────────────────────────────────────────────────

    private readonly int[] _turnOrder = new int[PlayerCount];
    private int   _turnOrderIndex;
    private float _turnEndTime;
    private bool  _turnTimerActive;

    // ── Unity ─────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        if (!_turnTimerActive) return;
        if (TimeLeft <= 0f)
        {
            _turnTimerActive = false;
            HandleTimerExpired();
        }
    }

    // ── Public API ────────────────────────────────────────────────────

    /// <summary>Lance la roulette initiale, puis démarre le premier tour.</summary>
    public void StartRoulette()
    {
        StartCoroutine(RouletteCoroutine());
    }

    /// <summary>Demande un secouage. Retourne false si refusé (InputBlocked ou bouteille vide).</summary>
    public bool RequestShake()
    {
        if (InputBlocked) return false;
        if (GameManager.Instance.Bottle.CurrentPV <= 0) return false;
        StartCoroutine(ShakeCoroutine());
        return true;
    }

    /// <summary>Passe le tour. Requiert au minimum 1 secouage effectué.</summary>
    public bool RequestPassTurn()
    {
        if (InputBlocked || ShakesThisTurn < 1) return false;
        EndTurn();
        return true;
    }

    // ── Roulette ──────────────────────────────────────────────────────

    private IEnumerator RouletteCoroutine()
    {
        InputBlocked = true;

        // 1. Le gagnant est tiré au sort AVANT l'animation
        int winner = UnityEngine.Random.Range(0, PlayerCount);

        // 2. Calcul du nombre de pas total pour atterrir sur winner
        //    après rouletteMinFullLoops tours complets
        int totalSteps    = rouletteMinFullLoops * PlayerCount;
        int posAfterLoops = totalSteps % PlayerCount;                          // toujours 0 si loops entiers
        int stepsToWinner = (winner - posAfterLoops + PlayerCount) % PlayerCount;
        if (stepsToWinner == 0) stepsToWinner = PlayerCount;                  // au moins 1 pas supplémentaire
        totalSteps += stepsToWinner;

        // 3. Animation : ease-out garanti, atterrissage sur winner
        int currentArrow = 0;
        for (int step = 0; step < totalSteps; step++)
        {
            currentArrow = (currentArrow + 1) % PlayerCount;
            OnRouletteUpdate?.Invoke(currentArrow);

            float progress = (float)step / totalSteps;
            float interval = Mathf.Lerp(rouletteIntervalMin, rouletteIntervalMax, progress * progress);
            yield return new WaitForSeconds(interval);
        }

        // currentArrow == winner est garanti mathématiquement

        // 4. Construire l'ordre de tour (sens horaire depuis le gagnant)
        for (int i = 0; i < PlayerCount; i++)
            _turnOrder[i] = (winner + i) % PlayerCount;

        _turnOrderIndex = 0;
        CurrentHolder   = _turnOrder[0];

        InputBlocked = false;
        BeginTurn();
    }

    // ── Tour ──────────────────────────────────────────────────────────

    private void BeginTurn()
    {
        ShakesThisTurn   = 0;
        _turnEndTime     = Time.time + GameManager.Instance.TurnDuration;
        _turnTimerActive = true;

        OnTurnStarted?.Invoke(CurrentHolder);

        if (CurrentHolder != HumanPlayerIndex)
            AIController.Instance?.StartAITurn();
    }

    private void HandleTimerExpired()
    {
        if (ShakesThisTurn == 0)
            StartCoroutine(ForceShakeThenEndTurn()); // secouage pénalité
        else
            EndTurn();
    }

    private IEnumerator ForceShakeThenEndTurn()
    {
        yield return ShakeCoroutine();
        if (GameManager.Instance.CurrentPhase == GamePhase.Playing)
            EndTurn();
    }

    private void EndTurn()
    {
        _turnTimerActive = false;
        OnTurnEnded?.Invoke();

        _turnOrderIndex = (_turnOrderIndex + 1) % PlayerCount;
        CurrentHolder   = _turnOrder[_turnOrderIndex];
        BeginTurn();
    }

    // ── Secouage ──────────────────────────────────────────────────────

    private IEnumerator ShakeCoroutine()
    {
        InputBlocked = true;

        GameManager.Instance.Bottle.Shake();
        ShakesThisTurn++;
        OnShakePerformed?.Invoke();

        if (GameManager.Instance.Bottle.CurrentPV <= 0)
        {
            InputBlocked = false;
            yield break;
        }

        // Le timer continue dans Update() — cette attente ne le bloque plus
        yield return new WaitForSeconds(GameManager.Instance.ShakeDuration);

        InputBlocked = false;
    }
}
