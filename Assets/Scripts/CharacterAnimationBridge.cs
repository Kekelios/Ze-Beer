using UnityEngine;

public class CharacterAnimationBridge : MonoBehaviour
{
    [Header("Controllers (index = playerIndex : 0=P1, 1=AI1, 2=AI2, 3=AI3)")]
    [SerializeField] private Character2DAnimController[] controllers;

    private BottleState _currentState = BottleState.Fresh;
    private BottleType _currentType = BottleType.Cola;
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

    private void HandlePhaseChanged(GamePhase phase)
    {
        if (phase != GamePhase.Playing) return;

        UnsubscribeBottle();
        _subscribedBottle = GameManager.Instance.Bottle;

        if (_subscribedBottle != null && _subscribedBottle.Data != null)
        {
            _subscribedBottle.OnStateChanged += HandleBottleStateChanged;
            _currentState = _subscribedBottle.State;
            _currentType = _subscribedBottle.Data.bottleType;
        }
        else
        {
            _currentState = BottleState.Fresh;
            _currentType = BottleType.Cola;
        }

        _currentHolder = -1;

        foreach (var c in controllers)
            c?.PlayIdle(_currentType, _currentState);
    }

    private void HandleTurnStarted(int holderIndex)
    {
        _currentHolder = holderIndex;

        // si on a changé de bouteille (rare), on refresh le type
        var bottle = GameManager.Instance.Bottle;
        if (bottle != null && bottle.Data != null)
            _currentType = bottle.Data.bottleType;

        for (int i = 0; i < controllers.Length; i++)
        {
            var c = controllers[i];
            if (c == null) continue;

            if (i == holderIndex) c.PlayHold(_currentType, _currentState);
            else c.PlayIdle(_currentType, _currentState);
        }
    }

    private void HandleShakeStarted()
    {
        if (!IsValidHolder()) return;
        controllers[_currentHolder].PlayShake(_currentType, _currentState);
    }

    private void HandleShakeCompleted()
    {
        if (!IsValidHolder()) return;

        if (TurnManager.Instance.CurrentHolder == _currentHolder)
            controllers[_currentHolder].PlayHold(_currentType, _currentState);
    }

    private void HandleBottleStateChanged(BottleState newState)
    {
        _currentState = newState;

        for (int i = 0; i < controllers.Length; i++)
        {
            var c = controllers[i];
            if (c == null) continue;

            // conserve la phase courante, mais met à jour l'état
            switch (c.CurrentPhase)
            {
                case AnimPhase.Idle: c.PlayIdle(_currentType, newState); break;
                case AnimPhase.Hold: c.PlayHold(_currentType, newState); break;
                case AnimPhase.Shake: c.PlayShake(_currentType, newState); break;
            }
        }
    }

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