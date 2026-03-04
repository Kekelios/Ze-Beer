using UnityEngine;

public class CharacterAnimationBridge : MonoBehaviour
{
    [Header("Controllers (index = playerIndex : 0=P1, 1=AI1, 2=AI2, 3=AI3)")]
    [SerializeField] private Character2DAnimController[] controllers;

    private BottleState _currentState;
    private BottleType  _currentType;
    private int         _currentHolder = -1;
    private BottleModel _subscribedBottle;

    private void Start()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnStarted    += HandleTurnStarted;
            TurnManager.Instance.OnShakePerformed += HandleShakeStarted;
            TurnManager.Instance.OnShakeCompleted += HandleShakeCompleted;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPhaseChanged += HandlePhaseChanged;

            if (GameManager.Instance.CurrentPhase == GamePhase.Playing &&
                GameManager.Instance.Bottle != null)
                HandlePhaseChanged(GamePhase.Playing);
        }
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

    // ── Phase ─────────────────────────────────────────────────────────

    private void HandlePhaseChanged(GamePhase phase)
    {
        if (phase == GamePhase.GameOver)
        {
            // Stoppe toutes les animations immédiatement
            foreach (var c in controllers)
                c?.StopAnimation();

            UnsubscribeBottle();
            return;
        }

        if (phase != GamePhase.Playing) return;

        UnsubscribeBottle();

        _subscribedBottle = GameManager.Instance.Bottle;

        if (_subscribedBottle == null || _subscribedBottle.Data == null)
        {
            Debug.LogError("[Bridge] Bottle non initialisée — vérifie l'ordre d'exécution des scripts.");
            return;
        }

        _subscribedBottle.OnStateChanged += HandleBottleStateChanged;

        _currentState = _subscribedBottle.State;
        _currentType  = _subscribedBottle.Data.bottleType;

        foreach (var c in controllers)
            c?.PlayIdle(_currentType, _currentState);
    }

    // ── Tour ──────────────────────────────────────────────────────────

    private void HandleTurnStarted(int holderIndex)
    {
        _currentHolder = holderIndex;

        var bottle = GameManager.Instance?.Bottle;
        if (bottle?.Data != null)
            _currentType = bottle.Data.bottleType;

        for (int i = 0; i < controllers.Length; i++)
        {
            var c = controllers[i];
            if (c == null) continue;

            if (i == holderIndex) c.PlayHold(_currentType, _currentState);
            else                  c.PlayIdle(_currentType, _currentState);
        }
    }

    // ── Secouage ──────────────────────────────────────────────────────

    private void HandleShakeStarted()
    {
        if (!IsValidHolder()) return;
        controllers[_currentHolder].PlayShake(_currentType, _currentState);

        bool isFemale = (_currentHolder == 1);
        SoundManager.Instance?.PlayReaction(isFemale);
    }

    private void HandleShakeCompleted()
    {
        if (!IsValidHolder()) return;

        if (TurnManager.Instance.CurrentHolder == _currentHolder)
            controllers[_currentHolder].PlayHold(_currentType, _currentState);
    }

    // ── État bouteille ────────────────────────────────────────────────

    private void HandleBottleStateChanged(BottleState newState)
    {
        _currentState = newState;

        foreach (var c in controllers)
        {
            if (c == null) continue;

            switch (c.CurrentPhase)
            {
                case AnimPhase.Idle:  c.PlayIdle (_currentType, newState); break;
                case AnimPhase.Hold:  c.PlayHold (_currentType, newState); break;
                case AnimPhase.Shake: c.PlayShake(_currentType, newState); break;
            }
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private bool IsValidHolder() =>
        _currentHolder >= 0 &&
        _currentHolder < controllers.Length &&
        controllers[_currentHolder] != null;

    private void UnsubscribeBottle()
    {
        if (_subscribedBottle == null) return;
        _subscribedBottle.OnStateChanged -= HandleBottleStateChanged;
        _subscribedBottle = null;
    }
}
