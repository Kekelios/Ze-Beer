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

    public int   CurrentHolder  { get; private set; }
    public int   ShakesThisTurn { get; private set; }
    public float TimeLeft       => Mathf.Max(0f, _turnEndTime - Time.unscaledTime);
    public bool  InputBlocked   { get; private set; }

    // ── Events ────────────────────────────────────────────────────────

    public event Action<int>   OnTurnStarted;
    public event Action<int>   OnRouletteUpdate;
    public event Action        OnShakePerformed;
    public event Action        OnShakeCompleted;
    public event Action        OnTurnEnded;
    public event Action<int[]> OnTurnOrderBuilt;

    // ── Internals ─────────────────────────────────────────────────────

    private readonly int[] _turnOrder = new int[PlayerCount];
    private int      _turnOrderIndex;
    private float    _turnEndTime;
    private bool     _turnTimerActive;
    private Coroutine _rouletteRoutine;

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

    /// <summary>Lance la roulette initiale puis démarre le premier tour.</summary>
    public void StartRoulette()
    {
        if (_rouletteRoutine != null)
            StopCoroutine(_rouletteRoutine);

        _rouletteRoutine = StartCoroutine(RouletteCoroutine());
    }

    /// <summary>Demande un secouage. Retourne false si refusé.</summary>
    public bool RequestShake()
    {
        if (InputBlocked)   return false;
        if (!_turnTimerActive) return false;
        if (GameManager.Instance.Bottle.CurrentPV <= 0) return false;

        StartCoroutine(ShakeCoroutine());
        SoundManager.Instance?.PlayShake();
        return true;
    }

    /// <summary>Passe le tour. Requiert au minimum 1 secouage effectué.</summary>
    public bool RequestPassTurn()
    {
        if (InputBlocked)      return false;
        if (!_turnTimerActive) return false;
        if (ShakesThisTurn < 1) return false;

        EndTurn();
        return true;
    }

    /// <summary>Retourne une copie de l'ordre de tour actuel.</summary>
    public int[] GetTurnOrderCopy()
    {
        var copy = new int[PlayerCount];
        Array.Copy(_turnOrder, copy, PlayerCount);
        return copy;
    }

    /// <summary>
    /// Arrête définitivement le système de tour (appelé par GameManager à l'explosion).
    /// Bloque tout input, stoppe le timer et interrompt le tour IA en cours.
    /// </summary>
    public void Shutdown()
    {
        StopAllCoroutines();
        _turnTimerActive = false;
        InputBlocked     = true;

        // Stoppe le coroutine IA qui tourne sur AIController
        AIController.Instance?.StopAITurn();
    }

    // ── Roulette ──────────────────────────────────────────────────────

    private IEnumerator RouletteCoroutine()
    {
        InputBlocked = true;

        int winner        = UnityEngine.Random.Range(0, PlayerCount);
        int totalSteps    = rouletteMinFullLoops * PlayerCount;
        int stepsToWinner = (winner - (totalSteps % PlayerCount) + PlayerCount) % PlayerCount;
        if (stepsToWinner == 0) stepsToWinner = PlayerCount;
        totalSteps += stepsToWinner;

        int currentArrow = 0;

        for (int step = 0; step < totalSteps; step++)
        {
            currentArrow = (currentArrow + 1) % PlayerCount;
            OnRouletteUpdate?.Invoke(currentArrow);

            float progress = (float)step / totalSteps;
            float interval = Mathf.Lerp(rouletteIntervalMin, rouletteIntervalMax, progress * progress);
            yield return new WaitForSecondsRealtime(interval);
        }

        for (int i = 0; i < PlayerCount; i++)
            _turnOrder[i] = (winner + i) % PlayerCount;

        _turnOrderIndex = 0;
        CurrentHolder   = _turnOrder[0];

        OnTurnOrderBuilt?.Invoke(GetTurnOrderCopy());

        InputBlocked = false;
        BeginTurn();
    }

    // ── Tour ──────────────────────────────────────────────────────────

    private void BeginTurn()
    {
        InputBlocked   = false;
        ShakesThisTurn = 0;

        _turnEndTime     = Time.unscaledTime + GameManager.Instance.TurnDuration;
        _turnTimerActive = true;

        OnTurnStarted?.Invoke(CurrentHolder);

        if (CurrentHolder != HumanPlayerIndex)
            AIController.Instance?.StartAITurn();
    }

    private void HandleTimerExpired()
    {
        if (ShakesThisTurn == 0)
            StartCoroutine(ForceShakeThenEndTurn());
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

        SoundManager.Instance?.PlayTakeBottle();
        BeginTurn();
    }

    // ── Secouage ──────────────────────────────────────────────────────

    private IEnumerator ShakeCoroutine()
    {
        InputBlocked = true;

        GameManager.Instance.Bottle.Shake();
        ShakesThisTurn++;

        // Si la bouteille vient d'exploser, GameManager.HandleExplosion a déjà
        // appelé Shutdown() — on ne déclenche aucun event pour éviter de
        // relancer animations et sons après la fin de partie.
        if (GameManager.Instance.Bottle.CurrentPV <= 0)
            yield break;

        OnShakePerformed?.Invoke();

        yield return new WaitForSecondsRealtime(GameManager.Instance.ShakeDuration);

        OnShakeCompleted?.Invoke();
        InputBlocked = false;
    }
}
