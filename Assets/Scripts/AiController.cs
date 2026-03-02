using System.Collections;
using UnityEngine;

public class AIController : MonoBehaviour
{
    public static AIController Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>Lance le tour de l'IA courante.</summary>
    public void StartAITurn()
    {
        StartCoroutine(AITurnCoroutine());
    }

    private IEnumerator AITurnCoroutine()
    {
        var bottle = GameManager.Instance.Bottle;
        var data   = bottle.Data;

        int shakesToDo = GetShakeCount(bottle);
        float turnDuration = GameManager.Instance.TurnDuration;
        float shakeDuration = GameManager.Instance.ShakeDuration;

        // Délai initial : l'IA "réfléchit" un peu
        yield return new WaitForSeconds(Random.Range(0.5f, 1.5f));

        int performed = 0;
        while (performed < shakesToDo)
        {
            float elapsed = turnDuration - TurnManager.Instance.TimeLeft;
            float remaining = TurnDuration() - elapsed;

            // Assez de temps pour un secouage + marge ?
            if (remaining < shakeDuration + 0.5f) break;

            bool shook = TurnManager.Instance.RequestShake();
            if (shook)
            {
                performed++;
                yield return new WaitForSeconds(shakeDuration + Random.Range(0.2f, 0.6f));
            }
            else
            {
                yield return new WaitForSeconds(0.1f);
            }
        }
        // L'IA passe toujours son tour après ses secouages
        // Le TurnManager gère la fin de timer automatiquement
    }

    private int GetShakeCount(BottleModel bottle)
    {
        var state = bottle.State;
        var data  = bottle.Data;
        return state switch
        {
            BottleState.Fresh => Random.Range(data.aiShakesFresh.x,  data.aiShakesFresh.y + 1),
            BottleState.Used  => Random.Range(data.aiShakesUsed.x,   data.aiShakesUsed.y  + 1),
            BottleState.Crack => Random.Range(data.aiShakesCrack.x,  data.aiShakesCrack.y + 1),
            _                 => 1
        };
    }

    private float TurnDuration() => GameManager.Instance.TurnDuration;
}
