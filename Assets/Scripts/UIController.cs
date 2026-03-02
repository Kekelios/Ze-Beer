using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject panelMenu;
    [SerializeField] private GameObject panelBottleSelection;
    [SerializeField] private GameObject panelHUD;
    [SerializeField] private GameObject panelGameOver;

    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI[] playerNameLabels; // P1, AI1, AI2, AI3
    [SerializeField] private Image   bottleImage;
    [SerializeField] private TextMeshProUGUI timerLabel;
    [SerializeField] private Button  shakeButton;
    [SerializeField] private Button  passButton;
    [SerializeField] private Image   rouletteArrow;          // pointe vers le joueur actif
    [SerializeField] private RectTransform[] playerSlots;    // positions des 4 joueurs en UI

    [Header("Game Over")]
    [SerializeField] private TextMeshProUGUI gameOverLabel;

    [Header("Bottle Selection")]
    [SerializeField] private Button[] bottleButtons; // 0=Beer,1=ZebiCola,2=Champagne

    private PlayerController _playerController;

    private void Start()
    {
        _playerController = FindFirstObjectByType<PlayerController>();

        GameManager.Instance.OnPhaseChanged += HandlePhaseChange;
        TurnManager.Instance.OnShakePerformed   += RefreshBottleSprite;
        TurnManager.Instance.OnTurnStarted      += HandleTurnStarted;
        TurnManager.Instance.OnRouletteUpdate   += HandleRouletteUpdate;

        // Boutons
        shakeButton.onClick.AddListener(_playerController.OnShakeButton);
        passButton.onClick.AddListener(_playerController.OnPassTurnButton);

        for (int i = 0; i < bottleButtons.Length; i++)
        {
            int index = i;
            bottleButtons[i].onClick.AddListener(() => GameManager.Instance.StartGame(index));
        }

        HandlePhaseChange(GameManager.Instance.CurrentPhase);
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentPhase != GamePhase.Playing) return;

        timerLabel.text = Mathf.CeilToInt(TurnManager.Instance.TimeLeft).ToString();

        bool isPlayerTurn = TurnManager.Instance.CurrentHolder == 0 &&
                            !TurnManager.Instance.InputBlocked;
        shakeButton.interactable = isPlayerTurn;
        passButton.interactable  = isPlayerTurn && TurnManager.Instance.ShakesThisTurn >= 1;
    }

    // ── Phase changes ────────────────────────────────────────────────

    private void HandlePhaseChange(GamePhase phase)
    {
        panelMenu.SetActive(phase == GamePhase.Menu);
        panelBottleSelection.SetActive(phase == GamePhase.BottleSelection);
        panelHUD.SetActive(phase == GamePhase.Playing);
        panelGameOver.SetActive(phase == GamePhase.GameOver);

        if (phase == GamePhase.Playing)
        {
            RefreshBottleSprite();
            TurnManager.Instance.StartRoulette();
        }

        if (phase == GamePhase.GameOver)
        {
            gameOverLabel.text = GameManager.Instance.PlayerWon ? "VICTOIRE !" : "DÉFAITE...";
        }
    }

    // ── Bottle visual ────────────────────────────────────────────────

    private void RefreshBottleSprite()
    {
        var bottle = GameManager.Instance.Bottle;
        bottleImage.sprite = bottle.State switch
        {
            BottleState.Fresh => bottle.Data.spriteNew,
            BottleState.Used  => bottle.Data.spriteUsed,
            BottleState.Crack => bottle.Data.spriteCrack,
            _                 => bottle.Data.spriteNew
        };
    }

    // ── Turn / Roulette ──────────────────────────────────────────────

    private void HandleTurnStarted(int holder)
    {
        for (int i = 0; i < playerNameLabels.Length; i++)
        {
            var col = playerNameLabels[i].color;
            col.a = (i == holder) ? 1f : 0.4f;
            playerNameLabels[i].color = col;
        }
        PointArrowTo(holder);
    }

    private void HandleRouletteUpdate(int arrow)
    {
        PointArrowTo(arrow);
    }

    private void PointArrowTo(int slotIndex)
    {
        if (playerSlots == null || slotIndex >= playerSlots.Length) return;
        // Oriente la flèche vers le slot du joueur désigné
        Vector2 dir = (Vector2)playerSlots[slotIndex].position -
                      (Vector2)rouletteArrow.rectTransform.position;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        rouletteArrow.rectTransform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    // ── Menu buttons ─────────────────────────────────────────────────

    public void OnPlayButton()       => GameManager.Instance.GoToBottleSelection();
    public void OnReturnMenuButton() => GameManager.Instance.ReturnToMenu();
}
