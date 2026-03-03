using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Joue des animations flipbook 2D sur un SpriteRenderer ou une UI Image.
/// Un composant par personnage. Ne connaît pas le gameplay — reçoit uniquement
/// des ordres PlayIdle / PlayHold / PlayShake.
/// </summary>
public class Character2DAnimController : MonoBehaviour
{
    [Header("Rendu — assigne l'un ou l'autre")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Image          spriteImage;

    [Header("Animations")]
    [SerializeField] private Character2DAnimSet animSet;

    public BottleState CurrentState { get; private set; }
    public AnimPhase   CurrentPhase { get; private set; }

    private Coroutine _flipbook;

    // ── API publique ──────────────────────────────────────────────────

    /// <summary>Joue l'animation Idle pour l'état de bouteille donné.</summary>
    public void PlayIdle(BottleState state)  => Play(state, AnimPhase.Idle);

    /// <summary>Joue l'animation Hold pour l'état de bouteille donné.</summary>
    public void PlayHold(BottleState state)  => Play(state, AnimPhase.Hold);

    /// <summary>Joue l'animation Shake pour l'état de bouteille donné.</summary>
    public void PlayShake(BottleState state) => Play(state, AnimPhase.Shake);

    // ── Privé ─────────────────────────────────────────────────────────

    private void Play(BottleState state, AnimPhase phase)
    {
        CurrentState = state;
        CurrentPhase = phase;

        if (_flipbook != null) StopCoroutine(_flipbook);
        _flipbook = StartCoroutine(FlipbookLoop(state, phase));
    }

    /// <summary>
    /// Boucle infinie sur les frames — arrêtée uniquement par un appel à Play().
    /// Toutes les phases bouclent : le Bridge décide quand changer d'état.
    /// </summary>
    private IEnumerator FlipbookLoop(BottleState state, AnimPhase phase)
    {
        Sprite[] frames = animSet?.GetFrames(state, phase);

        if (frames == null || frames.Length == 0)
        {
            Debug.LogWarning($"[Character2DAnimController] Aucune frame pour {state}/{phase} sur {gameObject.name}.");
            yield break;
        }

        float interval = 1f / Mathf.Max(1f, animSet.GetFps(phase));
        int   index    = 0;

        while (true)
        {
            SetSprite(frames[index]);
            index = (index + 1) % frames.Length;
            yield return new WaitForSeconds(interval);
        }
    }

    private void SetSprite(Sprite sprite)
    {
        if      (spriteRenderer != null) spriteRenderer.sprite = sprite;
        else if (spriteImage    != null) spriteImage.sprite    = sprite;
    }
}
