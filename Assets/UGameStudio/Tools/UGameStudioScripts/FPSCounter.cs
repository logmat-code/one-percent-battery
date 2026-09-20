using UnityEngine;
using UnityEngine.UI;

public class FPSCounter : MonoBehaviour
{
    public Text fpsText;  // Drag & drop a Text component (from a Canvas) here to display the FPS

    private const float totalTime = 40.0f;  // Total duration for which the FPS will be monitored

    private float totalFrameTime = 0;       // Sum of all frame times over the totalTime duration
    private int totalFrames = 0;            // Total frames drawn over the totalTime duration
    private float elapsedTime = 0;          // Time passed since the start of monitoring
    public bool stop = false;

    private void Update()
    {
        elapsedTime += Time.deltaTime;
        if (stop && elapsedTime >= totalTime)
        {
            float averageFPS = totalFrames / totalFrameTime;
            Debug.Log($"Average FPS over {totalTime} seconds: {averageFPS:F2}");
            fpsText.text = $"Average FPS: {averageFPS:F2}";
            enabled = false;  // Disable this script after 20 seconds
            return;
        }

        totalFrameTime += Time.deltaTime;
        totalFrames++;

        // Display current FPS for this frame
        float currentFPS = 1.0f / Time.deltaTime;
        fpsText.text = $"FPS: {currentFPS:F0}";
    }
}
