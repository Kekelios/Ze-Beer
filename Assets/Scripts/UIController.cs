using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject panelHUD;
    [SerializeField] private GameObject panelGameOver;

    [Header("Turn Order Panel")]
    [SerializeField] private TextMeshProUGUI[] playerNameLabels;
    [SerializeField] private RectTransform rouletteArrow;

    [Header("HUD")]
    [SerializeField] private Image bottleImage;
    [SerializeField] private TextMeshProUGUI timerLabel;
    [SerializeField] private Button shakeButton;
    [SerializeField] private Button passButton;

    [Header("Debug HP (optionnel — laisser vide en prod)")]
    [SerializeField] private TextMeshProUGUI hpDebugLabel;

    [Header("Game Over")]
    [SerializeField] private TextMeshProUGUI gameOverLabel;
    [SerializeField] private Button restartButton;

    private static readonly string[] PlayerNames = { "P1", "AI1", "AI2", "AI3" };

    private int[] _turnOrder = new int[4];
    private int[] _uiPosByPlayerIndex = new int[4];
    private Vector3 _arrowTargetPosition;
    private PlayerController _playerController;

    private void Start()
    {
        _playerController = FindFirstObjectByType<PlayerController>();

        shakeButton.onClick.AddListener(_playerController.OnShakeButton);
        passButton.onClick.AddListener(_playerController.OnPassTurnButton);
        restartButton.onClick.AddListener(GameManager.Instance.GoToMainMenu);

        GameManager.Instance.OnPhaseChanged += HandlePhaseChange;
        TurnManager.Instance.OnTurnOrderBuilt += HandleTurnOrderBuilt;
        TurnManager.Instance.OnTurnStarted += HandleTurnStarted;
        TurnManager.Instance.OnRouletteUpdate += HandleRouletteUpdate;
        TurnManager.Instance.OnShakePerformed += RefreshBottleVisual;
        TurnManager.Instance.OnShakePerformed += RefreshHpDebug;

        if (rouletteArrow != null)
            _arrowTargetPosition = rouletteArrow.position;

        HandlePhaseChange(GameManager.Instance.CurrentPhase);
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentPhase != GamePhase.Playing) return;

        timerLabel.text = Mathf.CeilToInt(TurnManager.Instance.TimeLeft).ToString();

        bool isPlayerTurn = TurnManager.Instance.CurrentHolder == 0 &&
                            !TurnManager.Instance.InputBlocked;
        shakeButton.interactable = isPlayerTurn;
        passButton.interactable = isPlayerTurn && TurnManager.Instance.ShakesThisTurn >= 1;

        if (rouletteArrow != null)
        {
            rouletteArrow.position = Vector3.Lerp(
                rouletteArrow.position,
                _arrowTargetPosition,
                Time.deltaTime * 12f
            );
        }
    }

    // ── Phase changes ─────────────────────────────────────────────────

    private void HandlePhaseChange(GamePhase phase)
    {
        panelHUD.SetActive(phase == GamePhase.Playing);
        panelGameOver.SetActive(phase == GamePhase.GameOver);

        if (phase == GamePhase.Playing)
        {
            for (int i = 0; i < playerNameLabels.Length; i++)
                playerNameLabels[i].text = PlayerNames[i];

            RefreshBottleVisual();
            RefreshHpDebug();
            TurnManager.Instance.StartRoulette();
        }

        if (phase == GamePhase.GameOver)
            gameOverLabel.text = GameManager.Instance.PlayerWon ? "VICTOIRE !" : "DÉFAITE...";
    }

    // ── Bottle visual ─────────────────────────────────────────────────

    /// <summary>Met à jour le sprite selon l'état de la bouteille.</summary>
    private void RefreshBottleVisual()
    {
        if (bottleImage == null) return;
        var bottle = GameManager.Instance.Bottle;
        bottleImage.sprite = bottle.State switch
        {
            BottleState.Fresh => bottle.Data.spriteNew,
            BottleState.Used => bottle.Data.spriteUsed,
            BottleState.Crack => bottle.Data.spriteCrack,
            _ => bottle.Data.spriteNew
        };
    }

    /// <summary>Affiche les PV perdus pour le debug (masqué si hpDebugLabel non assigné).</summary>
    private void RefreshHpDebug()
    {
        if (hpDebugLabel == null) return;
        var bottle = GameManager.Instance.Bottle;
        if (bottle == null) return;

        int lost = bottle.CurrentMaxPV - bottle.CurrentPV;
        hpDebugLabel.text = $"PV perdus : {lost} / {bottle.CurrentMaxPV}";
    }

    // ── Turn order ────────────────────────────────────────────────────

    /// <summary>Réordonne les labels selon l'ordre de tour tiré par la roulette.</summary>
    private void HandleTurnOrderBuilt(int[] order)
    {
        if (order == null || order.Length < 4) return;
        _turnOrder = order;

        for (int uiPos = 0; uiPos < order.Length; uiPos++)
        {
            _uiPosByPlayerIndex[order[uiPos]] = uiPos;
            playerNameLabels[uiPos].text = PlayerNames[order[uiPos]];
        }

        PointArrowTo(0);
    }

    // ── Turn / Roulette ───────────────────────────────────────────────

    private void HandleTurnStarted(int holderPlayerIndex)
    {
        int uiPos = _uiPosByPlayerIndex[holderPlayerIndex];

        for (int i = 0; i < playerNameLabels.Length; i++)
        {
            var col = playerNameLabels[i].color;
            col.a = (i == uiPos) ? 1f : 0.4f;
            playerNameLabels[i].color = col;
        }

        PointArrowTo(uiPos);
    }

    private void HandleRouletteUpdate(int arrowPlayerIndex) => PointArrowTo(arrowPlayerIndex);

    /// <summary>Déplace la flèche verticalement pour s'aligner sur le label cible.</summary>
    private void PointArrowTo(int labelIndex)
    {
        if (playerNameLabels == null || labelIndex < 0 || labelIndex >= playerNameLabels.Length) return;
        if (rouletteArrow == null) return;

        Vector3 target = rouletteArrow.position;
        target.y = playerNameLabels[labelIndex].rectTransform.position.y;
        _arrowTargetPosition = target;
    }
}
