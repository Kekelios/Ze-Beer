using UnityEngine;

[CreateAssetMenu(fileName = "BottleData", menuName = "ZeBeer/Bottle Data")]
public class BottleData : ScriptableObject
{
    [Header("Identity")]
    public string bottleName;
    public Sprite spriteNew;
    public Sprite spriteUsed;
    public Sprite spriteCrack;

    [Header("Hit Points")]
    public int minPV = 6;
    public int maxPV = 9;

    [Header("State Thresholds (ratio 0–1)")]
    [Range(0f, 1f)] public float freshThreshold = 0.6f;
    [Range(0f, 1f)] public float crackThreshold = 0.3f;

    [Header("AI Shake Ranges by State")]
    public Vector2Int aiShakesFresh  = new(2, 4);
    public Vector2Int aiShakesUsed   = new(1, 3);
    public Vector2Int aiShakesCrack  = new(1, 2);
}
