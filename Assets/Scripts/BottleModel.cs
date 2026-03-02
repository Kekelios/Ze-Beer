using System;
using UnityEngine;

public enum BottleState { Fresh, Used, Crack }

public class BottleModel
{
    public BottleData Data { get; private set; }
    public int CurrentMaxPV { get; private set; }
    public int CurrentPV { get; private set; }

    public BottleState State => ComputeState();

    public event Action<BottleState> OnStateChanged;
    public event Action OnExploded;

    public void Initialize(BottleData data)
    {
        Data = data;
        CurrentMaxPV = UnityEngine.Random.Range(data.minPV, data.maxPV + 1);
        CurrentPV = CurrentMaxPV;
        Debug.Log($"[BottleModel] {data.bottleName} initialisée – PV: {CurrentPV}");
    }

    /// <summary>Retire 1 PV et déclenche l'explosion si nécessaire.</summary>
    public void Shake()
    {
        if (CurrentPV <= 0) return;

        var stateBefore = ComputeState();
        CurrentPV--;
        Debug.Log($"[BottleModel] Secouage – PV restants: {CurrentPV}/{CurrentMaxPV}");

        var stateAfter = ComputeState();
        if (stateAfter != stateBefore)
            OnStateChanged?.Invoke(stateAfter);

        if (CurrentPV <= 0)
            OnExploded?.Invoke();
    }

    private BottleState ComputeState()
    {
        float ratio = CurrentMaxPV > 0 ? (float)CurrentPV / CurrentMaxPV : 0f;
        if (ratio > Data.freshThreshold)  return BottleState.Fresh;
        if (ratio > Data.crackThreshold)  return BottleState.Used;
        return BottleState.Crack;
    }
}
