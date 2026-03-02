using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private bool IsPlayerTurn =>
        TurnManager.Instance != null &&
        TurnManager.Instance.CurrentHolder == 0 &&
        !TurnManager.Instance.InputBlocked &&
        GameManager.Instance.CurrentPhase == GamePhase.Playing;

    /// <summary>Appelé par le bouton Shake dans l'UI.</summary>
    public void OnShakeButton()
    {
        if (!IsPlayerTurn) return;
        TurnManager.Instance.RequestShake();
    }

    /// <summary>Appelé par le bouton Pass Turn dans l'UI.</summary>
    public void OnPassTurnButton()
    {
        if (!IsPlayerTurn) return;
        TurnManager.Instance.RequestPassTurn();
    }
}
