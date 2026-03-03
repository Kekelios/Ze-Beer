using UnityEngine;

/// <summary>
/// Bridge entre gameplay (TurnManager + BottleModel) et animations 2D.
/// Les Character2DAnimController ne connaissent pas les règles : ce script pilote leurs phases.
/// </summary>
public class CharacterAnimationBridge : MonoBehaviour
{
    [Header("Controllers (index = playerIndex : 0=P1, 1=AI1, 2=AI2, 3=AI3)")]
    [SerializeField] private Character2DAnimController[] controllers;

    private BottleState _currentState = BottleState.Fresh;
    private int _currentHolder = -1;

    private BottleModel _subscribedBottle;

    private void Start()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnStarted += HandleTurnStarted;
            TurnManager.Instance.OnShakePerformed += HandleShakeStarted;
            TurnManager.Instance.OnShakeCompleted += HandleShakeCompleted;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.OnPhaseChanged += HandlePhaseChanged;
    }

    private void OnDestroy()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnStarted -= HandleTurnStarted;
            TurnManager.Instance.OnShakePerformed -= HandleShakeStarted;
            TurnManager.Instance.OnShakeCompleted -= HandleShakeCompleted;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.OnPhaseChanged -= HandlePhaseChanged;

        UnsubscribeBottle();
    }

    // ── Phase change ────────────────────────────────────────────────

    private void HandlePhaseChanged(GamePhase phase)
    {
        if (phase != GamePhase.Playing) return;

        UnsubscribeBottle();

        _subscribedBottle = GameManager.Instance.Bottle;

        if (_subscribedBottle != null)
        {
            _subscribedBottle.OnStateChanged += HandleBottleStateChanged;
            _currentState = _subscribedBottle.State;
        }
        else
        {
            _currentState = BottleState.Fresh;
        }

        _currentHolder = -1;

        if (controllers == null) return;

        foreach (var c in controllers)
            c?.PlayIdle(_currentState);
    }

    // ── Turn events ─────────────────────────────────────────────────

    private void HandleTurnStarted(int holderIndex)
    {
        _currentHolder = holderIndex;

        if (controllers == null) return;

        for (int i = 0; i < controllers.Length; i++)
        {
            var c = controllers[i];
            if (c == null) continue;

            if (i == holderIndex)
                c.PlayHold(_currentState);
            else
                c.PlayIdle(_currentState);
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

        if (TurnManager.Instance.CurrentHolder == _currentHolder)
            controllers[_currentHolder].PlayHold(_currentState);
    }

    // ── Bottle state ────────────────────────────────────────────────

    private void HandleBottleStateChanged(BottleState newState)
    {
        _currentState = newState;

        if (controllers == null) return;

        for (int i = 0; i < controllers.Length; i++)
        {
            var c = controllers[i];
            if (c == null) continue;

            switch (c.CurrentPhase)
            {
                case AnimPhase.Idle:
                    c.PlayIdle(newState);
                    break;

                case AnimPhase.Hold:
                    c.PlayHold(newState);
                    break;

                case AnimPhase.Shake:
                    c.PlayShake(newState);
                    break;
            }
        }
    }

    // ── Utils ───────────────────────────────────────────────────────

    private bool IsValidHolder()
    {
        return controllers != null &&
               _currentHolder >= 0 &&
               _currentHolder < controllers.Length &&
               controllers[_currentHolder] != null;
    }

    private void UnsubscribeBottle()
    {
        if (_subscribedBottle != null)
        {
            _subscribedBottle.OnStateChanged -= HandleBottleStateChanged;
            _subscribedBottle = null;
        }
    }
}