using System.Collections;
using UnityEngine;

public class WordTraceSequencer : MonoBehaviour
{
    [SerializeField] private ParticleSystem traceParticles;

    /// <summary>
    /// Traces around the specified UI RectTransform starting from bottom-left corner clockwise.
    /// </summary>
    /// <param name="targetRect">The UI RectTransform of the completed word box.</param>
    /// <param name="duration">Total travel duration in seconds.</param>
    public void TraceWordBox(RectTransform targetRect, float duration = 0.8f)
    {
        StopAllCoroutines();
        StartCoroutine(TracePathRoutine(targetRect, duration));
    }

    private IEnumerator TracePathRoutine(RectTransform targetRect, float duration)
    {
        // Get four corner positions in world space
        Vector3[] worldCorners = new Vector3[4];
        targetRect.GetWorldCorners(worldCorners);

        // UI World Corners Order: 0 = Bottom-Left, 1 = Top-Left, 2 = Top-Right, 3 = Bottom-Right
        Vector3[] pathPoints = new Vector3[5]
        {
            worldCorners[0], // Bottom-Left
            worldCorners[1], // Top-Left
            worldCorners[2], // Top-Right
            worldCorners[3], // Bottom-Right
            worldCorners[0]  // Back to Bottom-Left
        };

        // Calculate segment lengths to keep speed uniform on rectangular dimensions
        float[] segmentLengths = new float[4];
        float totalPerimeter = 0f;
        for (int i = 0; i < 4; i++)
        {
            segmentLengths[i] = Vector3.Distance(pathPoints[i], pathPoints[i + 1]);
            totalPerimeter += segmentLengths[i];
        }

        // Initialize position & play particles
        transform.position = pathPoints[0];
        if (traceParticles != null)
        {
            traceParticles.Clear();
            traceParticles.Play();
        }

        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / duration);
            float currentDistance = progress * totalPerimeter;

            // Determine current active edge segment based on distance traveled
            float accumulatedDistance = 0f;
            for (int i = 0; i < 4; i++)
            {
                if (currentDistance <= accumulatedDistance + segmentLengths[i])
                {
                    float segmentProgress = (currentDistance - accumulatedDistance) / segmentLengths[i];
                    transform.position = Vector3.Lerp(pathPoints[i], pathPoints[i + 1], segmentProgress);
                    break;
                }
                accumulatedDistance += segmentLengths[i];
            }

            yield return null;
        }

        transform.position = pathPoints[4];

        // Stop emission and let existing trail particles naturally fade out
        if (traceParticles != null)
        {
            traceParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}