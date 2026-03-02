using System;
using System.Collections;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    private const int PlayerCount = 4;
    private const int PlayerIndex = 0;

    [Header("Roulette")]
    [SerializeField] private float rouletteMinDuration = 1.5f;
    [SerializeField] private float rouletteMaxDuration = 3f;

    public int CurrentHolder { get; private set; }
    public int ShakesThisTurn { get; private set; }
    public float TimeLeft { get; private set; }
    public bool InputBlocked { get; private set; }

    public event Action<int> OnTurnStarted;     // holder index
    public event Action<int> OnRouletteUpdate;  // current arrow index
    public event Action     OnShakePerformed;
    public event Action     OnTurnEnded;

    private Coroutine _turnCoroutine;
    private Coroutine _shakeCoroutine;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ── Public API ───────────────────────────────────────────────────

    /// <summary>Lance la roulette puis démarre le premier tour.</summary>
    public void StartRoulette()
    {
        StartCoroutine(RouletteCoroutine());
    }

    /// <summary>Demande un secouage (joueur ou IA).</summary>
    public bool RequestShake()
    {
        if (InputBlocked) return false;
        if (GameManager.Instance.Bottle.CurrentPV <= 0) return false;
        _shakeCoroutine = StartCoroutine(ShakeCoroutine());
        return true;
    }

    /// <summary>Passe le tour (joueur uniquement, si >= 1 secouage).</summary>
    public bool RequestPassTurn()
    {
        if (InputBlocked || ShakesThisTurn < 1) return false;
        EndTurn();
        return true;
    }

    // ── Roulette ─────────────────────────────────────────────────────

    private IEnumerator RouletteCoroutine()
    {
        InputBlocked = true;
        float duration  = UnityEngine.Random.Range(rouletteMinDuration, rouletteMaxDuration);
        float elapsed   = 0f;
        float interval  = 0.12f;
        int   arrow     = 0;

        while (elapsed < duration)
        {
            arrow = (arrow + 1) % PlayerCount;
            OnRouletteUpdate?.Invoke(arrow);
            yield return new WaitForSeconds(interval);
            elapsed += interval;
            interval = Mathf.Lerp(0.12f, 0.4f, elapsed / duration); // ralentit
        }

        CurrentHolder = arrow;
        InputBlocked = false;
        BeginTurn();
    }

    // ── Turn loop ────────────────────────────────────────────────────

    private void BeginTurn()
    {
        ShakesThisTurn = 0;
        TimeLeft = GameManager.Instance.TurnDuration;
        OnTurnStarted?.Invoke(CurrentHolder);

        if (CurrentHolder != PlayerIndex)
            AIController.Instance?.StartAITurn();

        _turnCoroutine = StartCoroutine(TurnTimerCoroutine());
    }

    private IEnumerator TurnTimerCoroutine()
    {
        while (TimeLeft > 0f)
        {
            yield return null;
            if (!InputBlocked) TimeLeft -= Time.deltaTime;
        }

        // Timer expiré
        if (ShakesThisTurn == 0)
            yield return ShakeCoroutine(); // secouage automatique
        else
            EndTurn();
    }

    private IEnumerator ShakeCoroutine()
    {
        InputBlocked = true;
        GameManager.Instance.Bottle.Shake();
        ShakesThisTurn++;
        OnShakePerformed?.Invoke();

        if (GameManager.Instance.Bottle.CurrentPV <= 0)
            yield break; // explosion gérée par GameManager

        yield return new WaitForSeconds(GameManager.Instance.ShakeDuration);
        InputBlocked = false;
    }

    private void EndTurn()
    {
        if (_turnCoroutine != null) StopCoroutine(_turnCoroutine);
        OnTurnEnded?.Invoke();
        CurrentHolder = (CurrentHolder + 1) % PlayerCount;
        BeginTurn();
    }
}
