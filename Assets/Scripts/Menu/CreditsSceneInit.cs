using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CreditsSceneInit : MonoBehaviour
{
    [Header("Navigation")]
    [SerializeField] private Button backToMenuButton;

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private void Start()
    {
        SoundManager.Instance?.PlayMusicCredits(); // ← corrigé

        backToMenuButton?.onClick.AddListener(GoToMainMenu);
    }

    /// <summary>Revient au main menu et enclenche la musique menu.</summary>
    private void GoToMainMenu()
    {
        SoundManager.Instance?.PlayMusicIngame();
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
