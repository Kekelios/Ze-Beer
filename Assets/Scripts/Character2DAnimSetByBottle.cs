using UnityEngine;

[System.Serializable]
public class Character2DAnimSetByBottle
{
    [Header("Cola")]
    public Character2DAnimSet cola;

    [Header("Beer")]
    public Character2DAnimSet beer;

    [Header("Champagne")]
    public Character2DAnimSet champagne;

    public Character2DAnimSet Get(BottleType type) => type switch
    {
        BottleType.Cola => cola,
        BottleType.Beer => beer,
        BottleType.Champagne => champagne,
        _ => cola
    };
}