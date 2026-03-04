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
        Debug.Log("CLICK SHAKE");

        if (TurnManager.Instance == null)
        {
            Debug.Log("TurnManager NULL");
            return;
        }

        Debug.Log("Holder = " + TurnManager.Instance.CurrentHolder);
        Debug.Log("Blocked = " + TurnManager.Instance.InputBlocked);
        Debug.Log("Phase = " + GameManager.Instance.CurrentPhase);

        bool result = TurnManager.Instance.RequestShake();
        Debug.Log("RequestShake result = " + result);
    }

    /// <summary>Appelé par le bouton Pass Turn dans l'UI.</summary>
    public void OnPassTurnButton()
    {
        if (!IsPlayerTurn) return;
        TurnManager.Instance.RequestPassTurn();
    }

}
