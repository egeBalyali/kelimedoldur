using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Previews the next level's category and starts the saved linear level.</summary>
[ExecuteAlways, DisallowMultipleComponent]
public class MainMenuController : MonoBehaviour
{
    [Serializable]
    public class CategoryTheme
    {
        public LevelCategory category;
        public Color tint = Color.white;
        public Sprite background;
    }

    [SerializeField] private TMP_Text playButtonText;
    [SerializeField] private Image playButtonImage;
    [SerializeField] private Image menuBackground;
    [SerializeField] private Sprite defaultBackground;
    [SerializeField] private LevelSequenceData levelSequence;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private GameObject levelCanvas;
    [Tooltip("Gameplay controls canvas, shown together with the level when Play is pressed.")]
    [SerializeField] private GameObject buttonCanvas;
    [SerializeField] private string levelPrefix = "Level ";
    [SerializeField] private CategoryTheme[] categoryThemes =
    {
        new CategoryTheme { category = LevelCategory.History, tint = new Color(1f, .88f, .7f) },
        new CategoryTheme { category = LevelCategory.Science, tint = new Color(.7f, 1f, .9f) },
        new CategoryTheme { category = LevelCategory.Sports, tint = new Color(1f, .8f, .7f) },
        new CategoryTheme { category = LevelCategory.Music, tint = new Color(.88f, .75f, 1f) },
        new CategoryTheme { category = LevelCategory.Movies, tint = new Color(1f, .75f, .88f) },
        new CategoryTheme { category = LevelCategory.Geography, tint = new Color(.7f, .9f, 1f) },
        new CategoryTheme { category = LevelCategory.GeneralKnowledge, tint = new Color(1f, 1f, .8f) }
    };
    private Button playButton;

    private void OnEnable()
    {
        PlayerLevelProgress.Changed += RefreshPlayButtonText;
        if (Application.isPlaying)
        {
            if (playButtonImage != null)
            {
                playButton = playButtonImage.GetComponent<Button>();
                if (playButton == null) playButton = playButtonImage.gameObject.AddComponent<Button>();
                playButton.targetGraphic = playButtonImage;
                playButton.onClick.AddListener(Play);
            }
            if (levelManager != null) levelManager.AllLevelsCompleted += ShowMainMenu;
            if (levelManager != null) levelManager.LivesChanged += RefreshPlayButtonText;
        }
        RefreshPlayButtonText();
    }

    private void OnDisable()
    {
        PlayerLevelProgress.Changed -= RefreshPlayButtonText;
        if (playButton != null) playButton.onClick.RemoveListener(Play);
        if (levelManager != null) levelManager.AllLevelsCompleted -= ShowMainMenu;
        if (levelManager != null) levelManager.LivesChanged -= RefreshPlayButtonText;
    }

    public void RefreshPlayButtonText()
    {
        int number = PlayerLevelProgress.LevelNumber;
        LevelData level = levelSequence != null ? levelSequence.GetLevel(number - 1) : null;
        if (playButtonText != null)
            playButtonText.text = level != null
                ? levelPrefix + number + "\n<size=45%>(" + LevelCategories.DisplayName(level.Category) + ")</size>"
                : (levelSequence != null && number > levelSequence.LevelCount ? "All levels complete" : "No level available");
        if (playButton != null) playButton.interactable = level != null && (levelManager == null || levelManager.CanPlay);
        CategoryTheme theme = level == null || categoryThemes == null ? null : Array.Find(categoryThemes, t => t != null && t.category == level.Category);
        if (playButtonImage != null) playButtonImage.color = theme != null ? theme.tint : Color.white;
        if (menuBackground != null)
            menuBackground.sprite = theme != null && theme.background != null ? theme.background : defaultBackground;
    }

    public void Play()
    {
        if (!Application.isPlaying || levelManager == null || levelSequence == null ||
            levelSequence.GetLevel(PlayerLevelProgress.LevelNumber - 1) == null || !levelManager.CanPlay) return;
        if (levelCanvas != null) levelCanvas.SetActive(true);
        if (buttonCanvas != null) buttonCanvas.SetActive(true);
        levelManager.ResumeSavedLevel();
        var canvas = GetComponent<Canvas>();
        if (canvas != null) canvas.enabled = false;
    }

    public void ShowMainMenu()
    {
        if (levelCanvas != null) levelCanvas.SetActive(false);
        if (buttonCanvas != null) buttonCanvas.SetActive(false);
        var canvas = GetComponent<Canvas>();
        if (canvas != null) canvas.enabled = true;
        RefreshPlayButtonText();
    }
}
