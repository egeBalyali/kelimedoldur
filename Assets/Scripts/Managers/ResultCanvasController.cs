using UnityEngine;
using UnityEngine.UI;

public class ResultCanvasController : MonoBehaviour
{
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private LevelFlowManager flowManager;
    [SerializeField] private MainMenuController mainMenu;
    [SerializeField] private GameObject winCanvas;
    [SerializeField] private GameObject loseCanvas;
    [SerializeField] private Image loseBackground;
    [SerializeField] private Sprite timeoutPicture;
    [SerializeField] private Sprite wrongTriesPicture;
    private Sprite defaultLosePicture;
    [SerializeField] private Canvas gameplayCanvas;
    [SerializeField] private Canvas controlsCanvas;
    [SerializeField] private Button winHomeButton;
    [SerializeField] private Button loseHomeButton;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private LevelScoreManager scoreManager;
    [SerializeField] private ScoreEffectController winScoreEffect;
    private bool showingResult;
    private bool gameplayWasEnabled, controlsWereEnabled;

    private void Awake()
    {
        if (loseBackground != null) defaultLosePicture = loseBackground.sprite;
        if (winCanvas != null) winCanvas.SetActive(false);
        if (loseCanvas != null) loseCanvas.SetActive(false);
    }

    private void OnEnable()
    {
        if (levelManager != null)
        {
            if (flowManager == null) levelManager.LevelWon += ShowWin;
            levelManager.LevelLoaded += OnLevelLoaded;
        }
        if (flowManager != null) flowManager.AnswerIncorrect += ShowWrongTries;
        if (flowManager != null) flowManager.LevelCompleted += ShowWin;
        if (flowManager != null) flowManager.TimeExpired += ShowTimeout;
        if (winHomeButton != null) winHomeButton.onClick.AddListener(Home);
        if (loseHomeButton != null) loseHomeButton.onClick.AddListener(Home);
        if (retryButton != null) retryButton.onClick.AddListener(Retry);
        if (nextLevelButton != null) nextLevelButton.onClick.AddListener(NextLevel);
    }

    private void OnDisable()
    {
        if (levelManager != null)
        {
            if (flowManager == null) levelManager.LevelWon -= ShowWin;
            levelManager.LevelLoaded -= OnLevelLoaded;
        }
        if (flowManager != null) flowManager.AnswerIncorrect -= ShowWrongTries;
        if (flowManager != null) flowManager.LevelCompleted -= ShowWin;
        if (flowManager != null) flowManager.TimeExpired -= ShowTimeout;
        if (winHomeButton != null) winHomeButton.onClick.RemoveListener(Home);
        if (loseHomeButton != null) loseHomeButton.onClick.RemoveListener(Home);
        if (retryButton != null) retryButton.onClick.RemoveListener(Retry);
        if (nextLevelButton != null) nextLevelButton.onClick.RemoveListener(NextLevel);
        Close();
    }

    private void ShowWin()
    {
        if (showingResult) return;
        int finalScore = scoreManager != null ? scoreManager.CurrentScore : 0;
        Show(true);
        if (winScoreEffect != null) winScoreEffect.ShowTotal(finalScore);
    }
    private void Update()
    {
        if (retryButton != null && levelManager != null)
            retryButton.interactable = levelManager.CanPlay;
    }
    private void ShowWrongTries() => ShowLose(wrongTriesPicture);
    private void ShowTimeout() => ShowLose(timeoutPicture);

    private void ShowLose(Sprite picture)
    {
        if (showingResult) return;
        if (loseBackground != null)
            loseBackground.sprite = picture != null ? picture : defaultLosePicture;
        Show(false);
    }
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
        GameObject result = won ? winCanvas : loseCanvas;
        if (result != null && result.TryGetComponent<Canvas>(out var canvas)) canvas.enabled = true;
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
        if (!showingResult || loseCanvas == null || !loseCanvas.activeSelf) return;
        if (levelManager == null || !levelManager.RetryFailedLevel()) return;
        Close();
    }

    public void NextLevel()
    {
        if (!showingResult || winCanvas == null || !winCanvas.activeSelf) return;
        Close();
        // Winning already saved the next position. The final level returns to the menu.
        if (levelManager != null) levelManager.ResumeSavedLevel();
    }
}
