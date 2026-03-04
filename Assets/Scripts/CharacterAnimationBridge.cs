using UnityEngine;

public class CharacterAnimationBridge : MonoBehaviour
{
    [SerializeField] private Character2DAnimController[] controllers;

    private BottleState _currentState;
    private BottleType _currentType;
    private int _currentHolder = -1;

    private BottleModel _subscribedBottle;

    private void Start()
    {
        TurnManager.Instance.OnTurnStarted += HandleTurnStarted;
        TurnManager.Instance.OnShakePerformed += HandleShakeStarted;
        TurnManager.Instance.OnShakeCompleted += HandleShakeCompleted;
        GameManager.Instance.OnPhaseChanged += HandlePhaseChanged;

        // Si la partie est déjà en Playing
        HandlePhaseChanged(GameManager.Instance.CurrentPhase);
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

        if (_subscribedBottle == null || _subscribedBottle.Data == null)
        {
            Debug.LogError("[Bridge] Bottle non initialisée.");
            return;
        }

        _subscribedBottle.OnStateChanged += HandleBottleStateChanged;

        _currentState = _subscribedBottle.State;
        _currentType = _subscribedBottle.Data.bottleType;

        Debug.Log($"[Bridge] Type actif = {_currentType}");

        foreach (var c in controllers)
            c?.PlayIdle(_currentType, _currentState);
    }

    private void HandleTurnStarted(int holderIndex)
    {
        _currentHolder = holderIndex;

        var bottle = GameManager.Instance.Bottle;
        if (bottle != null && bottle.Data != null)
            _currentType = bottle.Data.bottleType;

        for (int i = 0; i < controllers.Length; i++)
        {
            var c = controllers[i];
            if (c == null) continue;

            if (i == holderIndex)
                c.PlayHold(_currentType, _currentState);
            else
                c.PlayIdle(_currentType, _currentState);
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

        foreach (var c in controllers)
        {
            if (c == null) continue;

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
        return _currentHolder >= 0 &&
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