using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MenuUIController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject plan1Panel;
    [SerializeField] private GameObject plan2Panel;
    [SerializeField] private GameObject plan3Panel;

    [Header("Plan 1")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;

    [Header("Plan 2")]
    [SerializeField] private Button creditsButton;

    [Header("Plan 3")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button leftArrowButton;
    [SerializeField] private Button rightArrowButton;
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text plan3BottleLabel;

    private MenuFlowController _flow;

    private void Start()
    {
        _flow = MenuFlowController.Instance;
        if (_flow == null)
        {
            Debug.LogError("[MenuUIController] MenuFlowController.Instance is null.");
            return;
        }

        WireButtons();
        _flow.OnPlanChanged += OnPlanChanged;
        _flow.OnBottleSelected += _ => RefreshPlan3Label();

        // Sync with the current plan (already set by MenuFlowController.Start)
        OnPlanChanged(_flow.CurrentPlan);
    }

    private void OnDestroy()
    {
        if (_flow == null) return;
        _flow.OnPlanChanged -= OnPlanChanged;
    }

    private void WireButtons()
    {
        playButton?.onClick.AddListener(_flow.GoToPlan2);
        quitButton?.onClick.AddListener(_flow.QuitGame);
        creditsButton?.onClick.AddListener(_flow.LoadCredits);
        backButton?.onClick.AddListener(_flow.BackToPlan2);
        leftArrowButton?.onClick.AddListener(() => _flow.CycleBottle(-1));
        rightArrowButton?.onClick.AddListener(() => _flow.CycleBottle(1));
        startButton?.onClick.AddListener(_flow.StartGame);
    }

    private void OnPlanChanged(MenuPlan plan)
    {
        plan1Panel?.SetActive(plan == MenuPlan.Plan1);
        plan2Panel?.SetActive(plan == MenuPlan.Plan2);
        plan3Panel?.SetActive(plan == MenuPlan.Plan3);

        if (plan == MenuPlan.Plan3)
            RefreshPlan3Label();
    }

    /// <summary>Updates the Plan 3 label from the currently selected bottle. Call after cycling.</summary>
    public void RefreshPlan3Label()
    {
        if (plan3BottleLabel == null || _flow == null) return;
        plan3BottleLabel.SetText(_flow.Bottles[_flow.SelectedBottleIndex].GetLabel());
    }
}
