using UnityEngine;
using UnityEngine.UI;

public class ResultCanvasController : MonoBehaviour
{
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private LevelFlowManager flowManager;
    [SerializeField] private MainMenuController mainMenu;
    [SerializeField] private GameObject winCanvas;
    [SerializeField] private GameObject loseCanvas;
    [SerializeField] private Canvas gameplayCanvas;
    [SerializeField] private Canvas controlsCanvas;
    [SerializeField] private Button winHomeButton;
    [SerializeField] private Button loseHomeButton;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button nextLevelButton;
    private bool showingResult;
    private bool gameplayWasEnabled, controlsWereEnabled;

    private void Awake()
    {
        if (winCanvas != null) winCanvas.SetActive(false);
        if (loseCanvas != null) loseCanvas.SetActive(false);
    }

    private void OnEnable()
    {
        if (levelManager != null)
        {
            levelManager.LevelWon += ShowWin;
            levelManager.LevelLoaded += OnLevelLoaded;
        }
        if (flowManager != null) flowManager.AnswerIncorrect += ShowLose;
        if (winHomeButton != null) winHomeButton.onClick.AddListener(Home);
        if (loseHomeButton != null) loseHomeButton.onClick.AddListener(Home);
        if (retryButton != null) retryButton.onClick.AddListener(Retry);
        if (nextLevelButton != null) nextLevelButton.onClick.AddListener(NextLevel);
    }

    private void OnDisable()
    {
        if (levelManager != null)
        {
            levelManager.LevelWon -= ShowWin;
            levelManager.LevelLoaded -= OnLevelLoaded;
        }
        if (flowManager != null) flowManager.AnswerIncorrect -= ShowLose;
        if (winHomeButton != null) winHomeButton.onClick.RemoveListener(Home);
        if (loseHomeButton != null) loseHomeButton.onClick.RemoveListener(Home);
        if (retryButton != null) retryButton.onClick.RemoveListener(Retry);
        if (nextLevelButton != null) nextLevelButton.onClick.RemoveListener(NextLevel);
        Close();
    }

    private void ShowWin() => Show(true);
    private void ShowLose() => Show(false);
    private void OnLevelLoaded(int index) => Close();

    private void Show(bool won)
    {
        if (showingResult) return;
        showingResult = true;
        gameplayWasEnabled = gameplayCanvas != null && gameplayCanvas.enabled;
        controlsWereEnabled = controlsCanvas != null && controlsCanvas.enabled;
        if (gameplayCanvas != null) gameplayCanvas.enabled = false;
        if (controlsCanvas != null) controlsCanvas.enabled = false;
        if (winCanvas != null) winCanvas.SetActive(won);
        if (loseCanvas != null) loseCanvas.SetActive(!won);
    }

    private void Close()
    {
        if (winCanvas != null) winCanvas.SetActive(false);
        if (loseCanvas != null) loseCanvas.SetActive(false);
        if (!showingResult) return;
        showingResult = false;
        if (gameplayCanvas != null) gameplayCanvas.enabled = gameplayWasEnabled;
        if (controlsCanvas != null) controlsCanvas.enabled = controlsWereEnabled;
    }

    public void Home()
    {
        Close();
        if (mainMenu != null) mainMenu.ShowMainMenu();
    }

    public void Retry()
    {
        Close();
        if (levelManager != null) levelManager.ResetUnfinishedWords();
    }

    public void NextLevel()
    {
        if (!showingResult || winCanvas == null || !winCanvas.activeSelf) return;
        Close();
        // Winning already saved the next position. The final level returns to the menu.
        if (levelManager != null) levelManager.ResumeSavedLevel();
    }
}
