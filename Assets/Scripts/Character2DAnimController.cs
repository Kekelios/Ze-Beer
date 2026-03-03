using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Character2DAnimController : MonoBehaviour
{
    [Header("Rendu — assigne l'un ou l'autre")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Image spriteImage;

    [Header("Animations (par bouteille)")]
    [SerializeField] private Character2DAnimSetByBottle animByBottle;

    [Header("Timing")]
    [Tooltip("Si true, les flipbooks ignorent Time.timeScale (recommandé si ton jeu change le timeScale).")]
    [SerializeField] private bool useRealtime = true;

    public BottleState CurrentState { get; private set; }
    public AnimPhase CurrentPhase { get; private set; }
    public BottleType CurrentBottleType { get; private set; }

    private Coroutine _flipbook;

    // API
    public void PlayIdle(BottleType bottleType, BottleState state) => Play(bottleType, state, AnimPhase.Idle);
    public void PlayHold(BottleType bottleType, BottleState state) => Play(bottleType, state, AnimPhase.Hold);
    public void PlayShake(BottleType bottleType, BottleState state) => Play(bottleType, state, AnimPhase.Shake);

    private void Play(BottleType bottleType, BottleState state, AnimPhase phase)
    {
        CurrentBottleType = bottleType;
        CurrentState = state;
        CurrentPhase = phase;

        if (_flipbook != null) StopCoroutine(_flipbook);
        _flipbook = StartCoroutine(FlipbookLoop(bottleType, state, phase));
    }

    private IEnumerator FlipbookLoop(BottleType bottleType, BottleState state, AnimPhase phase)
    {
        var set = animByBottle != null ? animByBottle.Get(bottleType) : null;
        if (set == null)
        {
            Debug.LogWarning($"[Character2DAnimController] AnimSet manquant pour {gameObject.name} ({bottleType}).");
            yield break;
        }

        Sprite[] frames = set.GetFrames(state, phase);

        // Fallback : si Hold vide, on joue Idle
        if ((frames == null || frames.Length == 0) && phase == AnimPhase.Hold)
            frames = set.GetFrames(state, AnimPhase.Idle);

        if (frames == null || frames.Length == 0)
        {
            Debug.LogWarning($"[Character2DAnimController] Aucune frame pour {bottleType}/{state}/{phase} sur {gameObject.name}.");
            yield break;
        }

        float fps = set.GetFps(phase);
        float interval = 1f / Mathf.Max(1f, fps);
        int index = 0;

        while (true)
        {
            SetSprite(frames[index]);
            index = (index + 1) % frames.Length;

            if (useRealtime) yield return new WaitForSecondsRealtime(interval);
            else yield return new WaitForSeconds(interval);
        }
    }

    private void SetSprite(Sprite sprite)
    {
        if (spriteRenderer != null) spriteRenderer.sprite = sprite;
        else if (spriteImage != null) spriteImage.sprite = sprite;
    }
}