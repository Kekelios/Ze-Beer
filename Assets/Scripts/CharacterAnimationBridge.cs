using UnityEngine;

/// <summary>
/// Écoute les événements de TurnManager et BottleModel, puis pilote
/// les Character2DAnimController de chaque personnage.
/// Les controllers ne connaissent pas le gameplay — ce bridge fait le lien.
/// </summary>
public class CharacterAnimationBridge : MonoBehaviour
{
    [Header("Controllers (index = slot joueur : 0=P1, 1=AI1, 2=AI2, 3=AI3)")]
    [SerializeField] private Character2DAnimController[] controllers;

    private BottleState  _currentState  = BottleState.Fresh;
    private int          _currentHolder = -1;
    private BottleModel  _subscribedBottle;

    // ── Unity ─────────────────────────────────────────────────────────

    private void Start()
    {
        TurnManager.Instance.OnTurnStarted    += HandleTurnStarted;
        TurnManager.Instance.OnShakePerformed += HandleShakeStarted;
        TurnManager.Instance.OnShakeCompleted += HandleShakeCompleted;
        GameManager.Instance.OnPhaseChanged   += HandlePhaseChanged;
    }

    private void OnDestroy()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnStarted    -= HandleTurnStarted;
            TurnManager.Instance.OnShakePerformed -= HandleShakeStarted;
            TurnManager.Instance.OnShakeCompleted -= HandleShakeCompleted;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.OnPhaseChanged -= HandlePhaseChanged;

        UnsubscribeBottle();
    }

    // ── Handlers ──────────────────────────────────────────────────────

    private void HandlePhaseChanged(GamePhase phase)
    {
        if (phase != GamePhase.Playing) return;

        // Re-subscribe à chaque nouvelle partie (RestartGame crée un nouveau BottleModel)
        UnsubscribeBottle();
        _subscribedBottle = GameManager.Instance.Bottle;
        _subscribedBottle.OnStateChanged += HandleBottleStateChanged;

        _currentState  = _subscribedBottle.State;
        _currentHolder = -1;

        foreach (var c in controllers)
            c?.PlayIdle(_currentState);
    }

    private void HandleTurnStarted(int holderIndex)
    {
        _currentHolder = holderIndex;

        for (int i = 0; i < controllers.Length; i++)
        {
            if (controllers[i] == null) continue;

            if (i == holderIndex)
                controllers[i].PlayHold(_currentState);
            else
                controllers[i].PlayIdle(_currentState);
        }
    }

    private void HandleShakeStarted()
    {
        if (!IsValidHolder()) return;
        controllers[_currentHolder].PlayShake(_currentState);
    }

    private void HandleShakeCompleted()
    {
        if (!IsValidHolder()) return;

        // Si c'est toujours le tour de ce joueur → Hold, sinon OnTurnStarted du suivant gère tout
        if (TurnManager.Instance.CurrentHolder == _currentHolder)
            controllers[_currentHolder].PlayHold(_currentState);
    }

    private void HandleBottleStateChanged(BottleState newState)
    {
        _currentState = newState;

        // Chaque personnage garde sa phase actuelle, mais passe au nouvel état de bouteille
        for (int i = 0; i < controllers.Length; i++)
        {
            if (controllers[i] == null) continue;

            switch (controllers[i].CurrentPhase)
            {
                case AnimPhase.Idle:  controllers[i].PlayIdle(newState);  break;
                case AnimPhase.Hold:  controllers[i].PlayHold(newState);  break;
                case AnimPhase.Shake: controllers[i].PlayShake(newState); break;
            }
        }
    }

    // ── Utilitaires ───────────────────────────────────────────────────

    private bool IsValidHolder() =>
        _currentHolder >= 0 &&
        _currentHolder < controllers.Length &&
        controllers[_currentHolder] != null;

    private void UnsubscribeBottle()
    {
        if (_subscribedBottle != null)
        {
            _subscribedBottle.OnStateChanged -= HandleBottleStateChanged;
            _subscribedBottle = null;
        }
    }
}
