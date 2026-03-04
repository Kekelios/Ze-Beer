using System.Collections;
using UnityEngine;

public class AIController : MonoBehaviour
{
    public static AIController Instance { get; private set; }

    [Header("AI Timing (Realtime)")]
    [SerializeField] private Vector2 thinkDelayRange          = new Vector2(0.4f, 1.0f);
    [SerializeField] private Vector2 betweenShakesDelayRange  = new Vector2(0.15f, 0.35f);
    [SerializeField] private float   safetyMarginSeconds      = 0.25f;

    [Header("AI Pass chance AFTER a completed shake (by bottle state)")]
    [Range(0f, 1f)][SerializeField] private float passChanceFresh = 0.10f;
    [Range(0f, 1f)][SerializeField] private float passChanceUsed  = 0.18f;
    [Range(0f, 1f)][SerializeField] private float passChanceCrack = 0.30f;

    [Header("Rig randomness (reduce full shakes)")]
    [Tooltip("Plus c'est élevé, plus l'IA est biaisée vers le MIN de la range. 1 = uniforme.")]
    [SerializeField] private float lowBiasPower = 2.2f;

    [Tooltip("Chance additionnelle de stopper tôt (après 1+ shake) même si la cible n'est pas atteinte.")]
    [Range(0f, 1f)][SerializeField] private float earlyStopFresh = 0.10f;
    [Range(0f, 1f)][SerializeField] private float earlyStopUsed  = 0.18f;
    [Range(0f, 1f)][SerializeField] private float earlyStopCrack = 0.28f;

    private Coroutine _aiRoutine;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>Démarre le tour IA pour le holder courant.</summary>
    public void StartAITurn()
    {
        if (_aiRoutine != null) StopCoroutine(_aiRoutine);
        _aiRoutine = StartCoroutine(AITurnCoroutine());
    }

    /// <summary>Interrompt immédiatement le tour IA en cours (appelé par TurnManager.Shutdown).</summary>
    public void StopAITurn()
    {
        if (_aiRoutine == null) return;
        StopCoroutine(_aiRoutine);
        _aiRoutine = null;
    }

    // ── Coroutine principale ──────────────────────────────────────────

    private IEnumerator AITurnCoroutine()
    {
        int myIndex = TurnManager.Instance.CurrentHolder;
        if (myIndex == 0) yield break; // sécurité — le slot 0 est le joueur humain

        yield return new WaitForSecondsRealtime(Random.Range(thinkDelayRange.x, thinkDelayRange.y));

        while (GameManager.Instance.CurrentPhase == GamePhase.Playing &&
               TurnManager.Instance.CurrentHolder == myIndex)
        {
            var bottle       = GameManager.Instance.Bottle;
            int shakesTarget = GetShakeTargetBiased(bottle);
            int performed    = 0;

            while (performed < shakesTarget)
            {
                if (GameManager.Instance.CurrentPhase != GamePhase.Playing) yield break;
                if (TurnManager.Instance.CurrentHolder != myIndex)          yield break;

                float remaining = TurnManager.Instance.TimeLeft;
                if (remaining < GameManager.Instance.ShakeDuration + safetyMarginSeconds)
                    break;

                bool shook = TurnManager.Instance.RequestShake();
                if (!shook)
                {
                    yield return new WaitForSecondsRealtime(0.05f);
                    continue;
                }

                performed++;

                yield return WaitUntilShakeFinishedOrTurnEnds(myIndex);

                if (GameManager.Instance.CurrentPhase != GamePhase.Playing) yield break;
                if (TurnManager.Instance.CurrentHolder != myIndex)          yield break;

                if (TurnManager.Instance.ShakesThisTurn >= 1)
                {
                    if (ShouldPassAfterShake(bottle.State) || ShouldEarlyStop(bottle.State))
                    {
                        TurnManager.Instance.RequestPassTurn();
                        yield break;
                    }
                }

                yield return new WaitForSecondsRealtime(
                    Random.Range(betweenShakesDelayRange.x, betweenShakesDelayRange.y));
            }

            if (GameManager.Instance.CurrentPhase == GamePhase.Playing &&
                TurnManager.Instance.CurrentHolder == myIndex          &&
                TurnManager.Instance.ShakesThisTurn >= 1              &&
                !TurnManager.Instance.InputBlocked)
            {
                TurnManager.Instance.RequestPassTurn();
            }

            yield break;
        }
    }

    private IEnumerator WaitUntilShakeFinishedOrTurnEnds(int myIndex)
    {
        while (GameManager.Instance.CurrentPhase == GamePhase.Playing &&
               TurnManager.Instance.CurrentHolder == myIndex          &&
               TurnManager.Instance.InputBlocked)
        {
            yield return null;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private bool ShouldPassAfterShake(BottleState state)
    {
        float p = state switch
        {
            BottleState.Fresh => passChanceFresh,
            BottleState.Used  => passChanceUsed,
            BottleState.Crack => passChanceCrack,
            _                 => 0.15f
        };
        return Random.value < p;
    }

    private bool ShouldEarlyStop(BottleState state)
    {
        float p = state switch
        {
            BottleState.Fresh => earlyStopFresh,
            BottleState.Used  => earlyStopUsed,
            BottleState.Crack => earlyStopCrack,
            _                 => 0.15f
        };
        return Random.value < p;
    }

    private int GetShakeTargetBiased(BottleModel bottle)
    {
        var data = bottle.Data;

        Vector2Int range = bottle.State switch
        {
            BottleState.Fresh => data.aiShakesFresh,
            BottleState.Used  => data.aiShakesUsed,
            BottleState.Crack => data.aiShakesCrack,
            _                 => new Vector2Int(1, 1)
        };

        int min = Mathf.Max(1, range.x);
        int max = Mathf.Max(min, range.y);

        float t     = Mathf.Pow(Random.value, Mathf.Max(1f, lowBiasPower));
        int   value = min + Mathf.FloorToInt(t * (max - min + 1));
        return Mathf.Clamp(value, min, max);
    }
}
