using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

/// <summary>Positive question-word feedback and negative answer-submit feedback on mobile.</summary>
[DisallowMultipleComponent]
public class HapticFeedbackManager : MonoBehaviour
{
    [SerializeField] private SentenceView sentenceView;
    [SerializeField] private LevelFlowManager flowManager;
    [SerializeField] private bool hapticsEnabled = true;
    [SerializeField] private bool logInEditor;

    private void OnEnable()
    {
        if (sentenceView == null) sentenceView = GetComponent<SentenceView>();
        if (sentenceView != null) sentenceView.CorrectWordTraceStarted += PlayHappy;
        if (flowManager != null) flowManager.AnswerIncorrect += PlaySad;
    }

    private void OnDisable()
    {
        if (sentenceView != null) sentenceView.CorrectWordTraceStarted -= PlayHappy;
        if (flowManager != null) flowManager.AnswerIncorrect -= PlaySad;
    }

    public void SetHapticsEnabled(bool value) => GameSettings.HapticsEnabled = value;

    [ContextMenu("Test Happy Haptic")]
    public void PlayHappy() => PlayFeedback(true);

    [ContextMenu("Test Sad Haptic")]
    public void PlaySad() => PlayFeedback(false);

    private void PlayFeedback(bool happy)
    {
        if (!isActiveAndEnabled || !hapticsEnabled || !GameSettings.HapticsEnabled || !Application.isPlaying) return;
#if UNITY_EDITOR
        if (logInEditor) Debug.Log(happy ? "[Haptics] Happy" : "[Haptics] Sad", this);
#elif UNITY_ANDROID
        PlayAndroid(happy);
#elif UNITY_IOS
        WordGame_NotificationHaptic(happy ? 1 : 0);
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static void PlayAndroid(bool happy)
    {
        try
        {
            using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                if (activity == null) return;
                activity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    try
                    {
                        using (var unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                        using (var current = unity.GetStatic<AndroidJavaObject>("currentActivity"))
                        using (var window = current.Call<AndroidJavaObject>("getWindow"))
                        using (var view = window.Call<AndroidJavaObject>("getDecorView"))
                        using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                        using (var constants = new AndroidJavaClass("android.view.HapticFeedbackConstants"))
                        {
                            string effect = version.GetStatic<int>("SDK_INT") >= 30
                                ? (happy ? "CONFIRM" : "REJECT")
                                : (happy ? "KEYBOARD_TAP" : "LONG_PRESS");
                            // No override flags: respect the device's touch-feedback setting.
                            view.Call<bool>("performHapticFeedback", constants.GetStatic<int>(effect));
                        }
                    }
                    catch (AndroidJavaException) { /* Unsupported/device activity unavailable: keep gameplay running. */ }
                }));
            }
        }
        catch (AndroidJavaException) { /* No mobile activity or haptics service. */ }
    }
#endif

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void WordGame_NotificationHaptic(int happy);
#endif
}
