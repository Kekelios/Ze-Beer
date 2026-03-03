using UnityEngine;

/// <summary>
/// Set complet de 9 animations 2D pour un personnage (3 états × 3 phases).
/// À remplir dans l'Inspector avec les frames de chaque flipbook.
/// </summary>
[System.Serializable]
public class Character2DAnimSet
{
    [Header("Fresh")]
    public Sprite[] freshIdleFrames;
    public Sprite[] freshHoldFrames;
    public Sprite[] freshShakeFrames;

    [Header("Used")]
    public Sprite[] usedIdleFrames;
    public Sprite[] usedHoldFrames;
    public Sprite[] usedShakeFrames;

    [Header("Crack")]
    public Sprite[] crackIdleFrames;
    public Sprite[] crackHoldFrames;
    public Sprite[] crackShakeFrames;

    [Header("Playback FPS")]
    public float fpsIdle  = 8f;
    public float fpsHold  = 8f;
    public float fpsShake = 12f;

    /// <summary>Retourne le tableau de frames correspondant à l'état et la phase donnés.</summary>
    public Sprite[] GetFrames(BottleState state, AnimPhase phase)
    {
        return (state, phase) switch
        {
            (BottleState.Fresh, AnimPhase.Idle)  => freshIdleFrames,
            (BottleState.Fresh, AnimPhase.Hold)  => freshHoldFrames,
            (BottleState.Fresh, AnimPhase.Shake) => freshShakeFrames,
            (BottleState.Used,  AnimPhase.Idle)  => usedIdleFrames,
            (BottleState.Used,  AnimPhase.Hold)  => usedHoldFrames,
            (BottleState.Used,  AnimPhase.Shake) => usedShakeFrames,
            (BottleState.Crack, AnimPhase.Idle)  => crackIdleFrames,
            (BottleState.Crack, AnimPhase.Hold)  => crackHoldFrames,
            (BottleState.Crack, AnimPhase.Shake) => crackShakeFrames,
            _                                    => freshIdleFrames
        };
    }

    /// <summary>Retourne le FPS correspondant à la phase donnée.</summary>
    public float GetFps(AnimPhase phase) => phase switch
    {
        AnimPhase.Idle  => fpsIdle,
        AnimPhase.Hold  => fpsHold,
        AnimPhase.Shake => fpsShake,
        _               => fpsIdle
    };
}
