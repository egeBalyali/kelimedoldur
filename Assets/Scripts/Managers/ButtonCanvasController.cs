using UnityEngine;
using UnityEngine.UI;

/// <summary>Home and retry actions for the gameplay controls canvas.</summary>
[DisallowMultipleComponent]
public class ButtonCanvasController : MonoBehaviour
{
    [SerializeField] private MainMenuController mainMenu;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private Button homeButton;
    [SerializeField] private Button retryButton;

    private void OnEnable()
    {
        if (homeButton != null) homeButton.onClick.AddListener(GoHome);
        if (retryButton != null) retryButton.onClick.AddListener(Retry);
    }

    private void OnDisable()
    {
        if (homeButton != null) homeButton.onClick.RemoveListener(GoHome);
        if (retryButton != null) retryButton.onClick.RemoveListener(Retry);
    }

    public void GoHome()
    {
        if (mainMenu != null) mainMenu.ShowMainMenu();
    }

    public void Retry()
    {
        // Preserve completed words on every page; return only unfinished/incorrect word tiles.
        if (levelManager != null) levelManager.ResetUnfinishedWords();
    }
}
