using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum MenuPlan { Plan1, Plan2 }

public class MenuFlowController : MonoBehaviour
{
    public static MenuFlowController Instance { get; private set; }

    [Header("Bottle Entries")]
    [SerializeField] private MenuBottleEntry[] bottles;

    [Header("Dependencies")]
    [SerializeField] private MenuCameraRig cameraRig;
    [SerializeField] private MenuUIController uiController;

    [Header("Scene Names")]
    [SerializeField] private string creditsSceneName = "Credits";

    public MenuPlan CurrentPlan { get; private set; } = MenuPlan.Plan1;
    public bool IsTransitioning { get; private set; }
    public MenuBottleEntry[] Bottles => bottles;

    public event Action<MenuPlan> OnPlanChanged;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        EnterPlan(MenuPlan.Plan1);
    }

    // ── Public API ────────────────────────────────────────────────────

    /// <summary>Transitions du Plan 1 au Plan 2.</summary>
    public void GoToPlan2()
    {
        if (IsTransitioning || CurrentPlan != MenuPlan.Plan1) return;
        StartCoroutine(Transition(MenuPlan.Plan2));
    }

    /// <summary>Lance la scène de jeu correspondant à la bouteille choisie.</summary>
    public void StartGameWithBottle(int bottleIndex)
    {
        if (IsTransitioning) return;

        bottleIndex = Mathf.Clamp(bottleIndex, 0, bottles.Length - 1);
        string sceneName = bottles[bottleIndex].gameSceneName;

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError($"[MenuFlowController] Aucune scène définie pour '{bottles[bottleIndex].displayName}'.");
            return;
        }

        // Passe l'index à GameManager s'il existe déjà (DontDestroyOnLoad)
        if (GameManager.Instance != null)
            GameManager.Instance.SetBottleIndex(bottleIndex);

        SceneManager.LoadScene(sceneName);
    }

    /// <summary>Charge la scène des crédits.</summary>
    public void LoadCredits()
    {
        if (IsTransitioning) return;
        if (string.IsNullOrEmpty(creditsSceneName))
        {
            Debug.LogError("[MenuFlowController] Nom de scène crédits non défini.");
            return;
        }
        SceneManager.LoadScene(creditsSceneName);
    }

    /// <summary>Quitte l'application.</summary>
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ── Private ───────────────────────────────────────────────────────

    private IEnumerator Transition(MenuPlan target)
    {
        IsTransitioning = true;
        yield return cameraRig.MoveToPlan(target);
        EnterPlan(target);
        IsTransitioning = false;
    }

    private void EnterPlan(MenuPlan plan)
    {
        CurrentPlan = plan;
        OnPlanChanged?.Invoke(plan);
    }
}
