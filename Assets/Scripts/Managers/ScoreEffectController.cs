using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Reusable score popup with canvas-rendered sparkle particles behind the text.</summary>
public class ScoreEffectController : MonoBehaviour
{
    [SerializeField] private LevelScoreManager scoreManager;
    [SerializeField] private RectTransform scoreEffect;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private Sprite sparkleSprite;
    [SerializeField, Min(0.1f)] private float duration = 1.1f;
    [SerializeField, Range(0.05f, 1f)] private float startScale = 0.2f;
    [SerializeField] private Vector3 travel = new Vector3(0f, 65f, -15f);
    [SerializeField, Range(4, 96)] private int particleCount = 24;
    [SerializeField] private bool countUpTotal;
    [SerializeField] private float particleSizeMultiplier = 1f;
    [SerializeField] private float particleSpreadMultiplier = 1f;

    private CanvasGroup group;
    private Vector3 restPosition;
    private Vector3 restScale;
    private float elapsed;
    private bool playing;
    private int displayedAward;
    private Image[] particles;
    private Vector2[] velocities;
    private float[] sizes;
    private Color[] colors;

    private void Awake()
    {
        if (scoreEffect == null) return;
        restPosition = scoreEffect.anchoredPosition3D;
        restScale = scoreEffect.localScale;
        group = scoreEffect.GetComponent<CanvasGroup>();
        if (group == null) group = scoreEffect.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
        if (scoreText != null) scoreText.raycastTarget = false;
        CreateParticles();
        Hide();
    }

    private void OnEnable()
    {
        if (scoreManager == null || countUpTotal) return;
        scoreManager.ScoreAwarded += Play;
        scoreManager.FeedbackCleared += Hide;
    }

    private void OnDisable()
    {
        if (scoreManager != null)
        {
            scoreManager.ScoreAwarded -= Play;
            scoreManager.FeedbackCleared -= Hide;
        }
        Hide();
    }

    private void CreateParticles()
    {
        // UI particles respect canvas ordering, so they stay behind the score on every canvas mode.
        var backdrop = new GameObject("HappyScoreParticles", typeof(RectTransform));
        backdrop.layer = scoreEffect.gameObject.layer;
        backdrop.transform.SetParent(scoreEffect, false);
        backdrop.transform.SetAsFirstSibling();
        particles = new Image[particleCount];
        velocities = new Vector2[particleCount];
        sizes = new float[particleCount];
        colors = new Color[particleCount];
        for (int i = 0; i < particleCount; i++)
        {
            var obj = new GameObject("Sparkle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.layer = scoreEffect.gameObject.layer;
            obj.transform.SetParent(backdrop.transform, false);
            particles[i] = obj.GetComponent<Image>();
            particles[i].sprite = sparkleSprite;
            particles[i].raycastTarget = false;
        }
    }

    private void Play(int points)
    {
        if (scoreEffect == null || group == null || (points <= 0 && !countUpTotal)) return;
        // Rapid rewards accumulate and replay from the original pose instead of drifting.
        displayedAward = playing ? displayedAward + points : points;
        if (scoreText != null) scoreText.text = countUpTotal ? "0" : $"+{displayedAward}";
        elapsed = 0f;
        playing = true;
        scoreEffect.anchoredPosition3D = restPosition;
        scoreEffect.localScale = restScale * startScale;
        group.alpha = 1f;
        for (int i = 0; i < particles.Length; i++)
        {
            float angle = (i + Random.value * 0.6f) / particles.Length * Mathf.PI * 2f;
            velocities[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Random.Range(100f, 220f) * particleSpreadMultiplier;
            sizes[i] = Random.Range(12f, 30f) * particleSizeMultiplier;
            colors[i] = Color.HSVToRGB(Random.Range(0.08f, 0.55f), 0.55f, 1f);
            particles[i].color = colors[i];
            particles[i].rectTransform.anchoredPosition = Vector2.zero;
            particles[i].rectTransform.sizeDelta = Vector2.one * sizes[i];
            particles[i].rectTransform.localRotation = Quaternion.identity;
        }
        scoreEffect.gameObject.SetActive(true);
    }

    public void ShowTotal(int points)
    {
        playing = false;
        Play(Mathf.Max(0, points));
    }

    private void Update()
    {
        if (!playing) return;
        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / Mathf.Max(0.1f, duration));
        float pop = Mathf.Clamp01(t / 0.3f);
        float scale = t < 0.3f
            ? Mathf.Lerp(startScale, 1.12f, 1f - Mathf.Pow(1f - pop, 3f))
            : Mathf.Lerp(1.12f, 1f, Mathf.Clamp01((t - 0.3f) / 0.25f));
        scoreEffect.localScale = restScale * scale;
        scoreEffect.anchoredPosition3D = restPosition + travel * (1f - Mathf.Pow(1f - t, 2f));
        group.alpha = countUpTotal ? 1f : 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.65f, 1f, t));
        if (countUpTotal && scoreText != null)
            scoreText.text = (t >= 1f ? displayedAward : Mathf.FloorToInt(displayedAward * (1f - Mathf.Pow(1f - t, 3f)))).ToString();
        for (int i = 0; i < particles.Length; i++)
        {
            var rect = particles[i].rectTransform;
            rect.anchoredPosition = velocities[i] * t + Vector2.down * (50f * t * t);
            rect.localRotation = Quaternion.Euler(0f, 0f, t * (i % 2 == 0 ? 150f : -150f));
            rect.sizeDelta = Vector2.one * sizes[i] * (1f - 0.7f * t);
            Color color = colors[i];
            color.a = 1f - t;
            particles[i].color = color;
        }
        if (t >= 1f)
        {
            if (countUpTotal) playing = false;
            else Hide();
        }
    }

    private void Hide()
    {
        playing = false;
        displayedAward = 0;
        if (scoreEffect == null) return;
        scoreEffect.gameObject.SetActive(false);
        if (group == null) return;
        scoreEffect.anchoredPosition3D = restPosition;
        scoreEffect.localScale = restScale;
        group.alpha = 1f;
    }
}
