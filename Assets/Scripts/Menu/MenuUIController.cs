using UnityEngine;
using UnityEngine.UI;

public class MenuUIController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject plan1Panel;
    [SerializeField] private GameObject plan2Panel;

    [Header("Plan 1")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;

    [Header("Plan 2 – Navigation")]
    [SerializeField] private Button creditsButton;

    [Header("Plan 2 – Sélection bouteille (index = ordre dans MenuFlowController.bottles)")]
    [SerializeField] private Button bottleButton0; // Beer
    [SerializeField] private Button bottleButton1; // ZebiCola
    [SerializeField] private Button bottleButton2; // Champagne

    private MenuFlowController _flow;

    private void Start()
    {
        _flow = MenuFlowController.Instance;
        if (_flow == null)
        {
            Debug.LogError("[MenuUIController] MenuFlowController.Instance est null.");
            return;
        }

        WireButtons();
        _flow.OnPlanChanged += OnPlanChanged;
        OnPlanChanged(_flow.CurrentPlan);
    }

    private void OnDestroy()
    {
        if (_flow != null)
            _flow.OnPlanChanged -= OnPlanChanged;
    }

    private void WireButtons()
    {
        playButton?.onClick.AddListener(_flow.GoToPlan2);
        quitButton?.onClick.AddListener(_flow.QuitGame);
        creditsButton?.onClick.AddListener(_flow.LoadCredits);

        bottleButton0?.onClick.AddListener(() => _flow.StartGameWithBottle(0));
        bottleButton1?.onClick.AddListener(() => _flow.StartGameWithBottle(1));
        bottleButton2?.onClick.AddListener(() => _flow.StartGameWithBottle(2));
    }

    private void OnPlanChanged(MenuPlan plan)
    {
        plan1Panel?.SetActive(plan == MenuPlan.Plan1);
        plan2Panel?.SetActive(plan == MenuPlan.Plan2);
    }
}
