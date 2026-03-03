using UnityEngine;

[CreateAssetMenu(fileName = "BottleData", menuName = "ZeBeer/Bottle Data")]
public class BottleData : ScriptableObject
{
    [Header("Identity")]
    public string bottleName;

    [Header("Hit Points")]
    public int minPV = 6;
    public int maxPV = 9;

    [Header("State Thresholds (ratio 0–1)")]
    [Range(0f, 1f)] public float freshThreshold = 0.6f;
    [Range(0f, 1f)] public float crackThreshold = 0.3f;

    [Header("AI Shake Ranges by State")]
    public Vector2Int aiShakesFresh = new(2, 4);
    public Vector2Int aiShakesUsed  = new(1, 3);
    public Vector2Int aiShakesCrack = new(1, 2);

    [Header("Bottle 2D Frames — État")]
    public Sprite[] freshFrames;
    public float     freshFPS = 8f;
    public Sprite[] usedFrames;
    public float     usedFPS  = 8f;
    public Sprite[] crackFrames;
    public float     crackFPS = 8f;

    // Sprites statiques legacy (gardés pour rétrocompatibilité UIController)
    [Header("Sprites statiques (legacy)")]
    public Sprite spriteNew;
    public Sprite spriteUsed;
    public Sprite spriteCrack;

    [Header("Explosion")]
    public Sprite[]   explosionFrames;
    public float      explosionFPS = 12f;
    public AudioClip  explosionSFX;

    /// <summary>Retourne les frames et le FPS correspondant à l'état donné.</summary>
    public (Sprite[] frames, float fps) GetStateAnim(BottleState state) => state switch
    {
        BottleState.Fresh => (freshFrames, freshFPS),
        BottleState.Used  => (usedFrames,  usedFPS),
        BottleState.Crack => (crackFrames, crackFPS),
        _                 => (freshFrames, freshFPS)
    };
}
