// TurnManager.cs
using System;
using System.Collections;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    private const int PlayerCount = 4;
    private const int HumanPlayerIndex = 0;

    [Header("Roulette")]
    [SerializeField] private int rouletteMinFullLoops = 2;
    [SerializeField] private float rouletteIntervalMin = 0.08f;
    [SerializeField] private float rouletteIntervalMax = 0.45f;

    // ── State ─────────────────────────────────────────────────────────

    public int CurrentHolder { get; private set; }
    public int ShakesThisTurn { get; private set; }

    // Unscaled => le timer ne s'arrête pas si Time.timeScale change
    public float TimeLeft => Mathf.Max(0f, _turnEndTime - Time.unscaledTime);

    public bool InputBlocked { get; private set; }

    // ── Events ────────────────────────────────────────────────────────

    /// <summary>playerIndex (0..3) du joueur actif</summary>
    public event Action<int> OnTurnStarted;

    /// <summary>playerIndex (0..3) surligné pendant la roulette</summary>
    public event Action<int> OnRouletteUpdate;

    /// <summary>déclenché au moment où le secouage est appliqué (PV-)</summary>
    public event Action OnShakePerformed;

    /// <summary>déclenché à la fin de la durée d'animation de secouage</summary>
    public event Action OnShakeCompleted;

    public event Action OnTurnEnded;

    /// <summary>ordre final (copie) en playerIndex, ex: [2,3,0,1]</summary>
    public event Action<int[]> OnTurnOrderBuilt;

    // ── Internals ─────────────────────────────────────────────────────

    private readonly int[] _turnOrder = new int[PlayerCount];
    private int _turnOrderIndex;
    private float _turnEndTime;
    private bool _turnTimerActive;

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

    /// <summary>Lance la roulette initiale, puis démarre le premier tour.</summary>
    public void StartRoulette()
    {
        if (_rouletteRoutine != null) StopCoroutine(_rouletteRoutine);
        _rouletteRoutine = StartCoroutine(RouletteCoroutine());
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

    public int[] GetTurnOrderCopy()
    {
        var copy = new int[PlayerCount];
        Array.Copy(_turnOrder, copy, PlayerCount);
        return copy;
    }

    // ── Roulette ──────────────────────────────────────────────────────

    private IEnumerator RouletteCoroutine()
    {
        InputBlocked = true;

        // 1) Le gagnant est tiré au sort AVANT l'animation
        int winner = UnityEngine.Random.Range(0, PlayerCount);

        // 2) Calcul du nombre de pas total pour atterrir sur winner après X tours complets
        int totalSteps = rouletteMinFullLoops * PlayerCount;
        int stepsToWinner = (winner - (totalSteps % PlayerCount) + PlayerCount) % PlayerCount;
        if (stepsToWinner == 0) stepsToWinner = PlayerCount; // au moins 1 tour de plus
        totalSteps += stepsToWinner;

        // 3) Animation : ease-out, atterrissage garanti sur winner
        int currentArrow = 0; // playerIndex en ordre FIXE 0..3
        for (int step = 0; step < totalSteps; step++)
        {
            currentArrow = (currentArrow + 1) % PlayerCount;
            OnRouletteUpdate?.Invoke(currentArrow);

            float progress = (float)step / totalSteps;
            float t = progress * progress; // ease-out simple
            float interval = Mathf.Lerp(rouletteIntervalMin, rouletteIntervalMax, t);

            // IMPORTANT : attendre l'interval, PAS ShakeDuration
            yield return new WaitForSecondsRealtime(interval);
        }

        // 4) Construire l'ordre de tour (sens horaire depuis le gagnant)
        for (int i = 0; i < PlayerCount; i++)
            _turnOrder[i] = (winner + i) % PlayerCount;

        _turnOrderIndex = 0;
        CurrentHolder = _turnOrder[_turnOrderIndex];

        // Prévenir l’UI : afficher P1/AI1/AI2/AI3 dans l'ordre de passage
        OnTurnOrderBuilt?.Invoke(GetTurnOrderCopy());

        InputBlocked = false;
        BeginTurn();
    }

    // ── Tour ──────────────────────────────────────────────────────────

    private void BeginTurn()
    {
        ShakesThisTurn = 0;

        _turnEndTime = Time.unscaledTime + GameManager.Instance.TurnDuration;
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
        CurrentHolder = _turnOrder[_turnOrderIndex];

        BeginTurn();
    }

    // ── Secouage ──────────────────────────────────────────────────────

    private IEnumerator ShakeCoroutine()
    {
        InputBlocked = true;

        // Applique le secouage (PV-)
        GameManager.Instance.Bottle.Shake();
        ShakesThisTurn++;
        OnShakePerformed?.Invoke();

        // Explosion => on quitte, pas de "completed"
        if (GameManager.Instance.Bottle.CurrentPV <= 0)
        {
            InputBlocked = false;
            yield break;
        }

        // L'animation dure ShakeDuration, sans dépendre du timeScale
        yield return new WaitForSecondsRealtime(GameManager.Instance.ShakeDuration);

        OnShakeCompleted?.Invoke();
        InputBlocked = false;
    }
}