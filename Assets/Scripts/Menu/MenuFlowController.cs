using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum MenuPlan { Plan1, Plan2, Plan3 }

public class MenuFlowController : MonoBehaviour
{
    public static MenuFlowController Instance { get; private set; }

    [Header("Bottle Entries")]
    [SerializeField] private MenuBottleEntry[] bottles;

    [Header("Dependencies")]
    [SerializeField] private MenuCameraRig cameraRig;
    [SerializeField] private BottleInteractor bottleInteractor;
    [SerializeField] private MenuUIController uiController;

    [Header("Scene Names")]
    [SerializeField] private string creditsSceneName = "Credits";

    public MenuPlan CurrentPlan { get; private set; } = MenuPlan.Plan1;
    public bool IsTransitioning { get; private set; }
    public int SelectedBottleIndex { get; private set; }
    public MenuBottleEntry[] Bottles => bottles;

    public event Action<MenuPlan> OnPlanChanged;
    public event Action<int> OnBottleSelected;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        for (int i = 0; i < bottles.Length; i++)
        {
            if (bottles[i].highlighter != null)
                bottles[i].highlighter.Initialize(i, bottles[i].GetLabel());
        }

        EnterPlan(MenuPlan.Plan1);
    }

    // ── Public API ────────────────────────────────────────────────────

    /// <summary>Begins the transition from Plan 1 to Plan 2.</summary>
    public void GoToPlan2()
    {
        if (IsTransitioning || CurrentPlan != MenuPlan.Plan1) return;
        StartCoroutine(Transition(MenuPlan.Plan2));
    }

    /// <summary>Selects a bottle by index and transitions to Plan 3.</summary>
    public void SelectBottleAndGoToPlan3(int index)
    {
        if (IsTransitioning || CurrentPlan != MenuPlan.Plan2) return;
        SelectedBottleIndex = Mathf.Clamp(index, 0, bottles.Length - 1);
        OnBottleSelected?.Invoke(SelectedBottleIndex);
        StartCoroutine(Transition(MenuPlan.Plan3));
    }

    /// <summary>Cycles the selected bottle by +1 or -1 (wrap-around) while in Plan 3.</summary>
    public void CycleBottle(int direction)
    {
        if (IsTransitioning || CurrentPlan != MenuPlan.Plan3) return;
        SelectedBottleIndex = (SelectedBottleIndex + direction + bottles.Length) % bottles.Length;
        OnBottleSelected?.Invoke(SelectedBottleIndex);
        StartCoroutine(MoveToPlan3Bottle());
    }

    /// <summary>Returns from Plan 3 to Plan 2.</summary>
    public void BackToPlan2()
    {
        if (IsTransitioning || CurrentPlan != MenuPlan.Plan3) return;
        StartCoroutine(Transition(MenuPlan.Plan2));
    }

    /// <summary>Loads the game scene for the selected bottle.</summary>
    public void StartGame()
    {
        if (IsTransitioning) return;
        string sceneName = bottles[SelectedBottleIndex].gameSceneName;
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError($"[MenuFlowController] No game scene set for '{bottles[SelectedBottleIndex].displayName}'.");
            return;
        }
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>Loads the Credits scene. Silently ignored during transitions.</summary>
    public void LoadCredits()
    {
        if (IsTransitioning) return;
        if (string.IsNullOrEmpty(creditsSceneName))
        {
            Debug.LogError("[MenuFlowController] Credits scene name is not set.");
            return;
        }
        SceneManager.LoadScene(creditsSceneName);
    }

    /// <summary>Quits the application. Stops play mode in the editor.</summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        Debug.Log("[MenuFlowController] Quit called — stopping play mode.");
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ── Private ───────────────────────────────────────────────────────

    private IEnumerator Transition(MenuPlan target)
    {
        IsTransitioning = true;
        bottleInteractor.SetEnabled(false);

        yield return cameraRig.MoveToPlan(target, SelectedBottleIndex);

        EnterPlan(target);
        IsTransitioning = false;
        bottleInteractor.SetEnabled(target == MenuPlan.Plan2);
    }

    private IEnumerator MoveToPlan3Bottle()
    {
        IsTransitioning = true;
        yield return cameraRig.MoveToPlan(MenuPlan.Plan3, SelectedBottleIndex);
        IsTransitioning = false;
        uiController.RefreshPlan3Label();
    }

    private void EnterPlan(MenuPlan plan)
    {
        CurrentPlan = plan;
        OnPlanChanged?.Invoke(plan);
    }
}
