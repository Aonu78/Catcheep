using System;
using System.Collections;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Activated only by a command-line flag in a dedicated QA build.
public sealed class CompatibilitySmokeTest : MonoBehaviour
{
    private int errors;
    private bool finished;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        if (!Debug.isDebugBuild && !Application.isEditor) return;
        if (!Environment.GetCommandLineArgs().Contains("-catcheep-smoke")) return;
        var host = new GameObject("Compatibility Smoke Test");
        DontDestroyOnLoad(host);
        host.AddComponent<CompatibilitySmokeTest>();
    }

    private void Awake()
    {
        if (Application.companyName == "CatcheepQA") PlayerPrefs.DeleteAll();
        Application.logMessageReceived += OnLog;
        Application.runInBackground = true;
        StartCoroutine(Watchdog());
    }

    private void OnLog(string message, string trace, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) errors++;
    }

    private IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(5);
        Check(SceneManager.GetActiveScene().name == "Introduction", "Opening reaches first-run introduction");
        yield return Capture("Introduction");
        var intro = FindAnyObjectByType<introManager>();
        if (intro != null)
        {
            for (int i = 0; i < 12; i++) { intro.forward(); yield return new WaitForSecondsRealtime(0.1f); }
        }
        yield return new WaitForSecondsRealtime(2);
        Check(SceneManager.GetActiveScene().name == "Start", "Introduction reaches menu");
        Check(FindAnyObjectByType<startMenuManager>() != null, "Menu manager starts");
        yield return Capture("Start");
        Check(Assets.Scripts.music.Instance != null, "Persistent audio service exists");
        Check(PlayerPrefs.GetInt("hearts") > 0, "First-run hearts initialized");
        PlayerPrefs.SetInt("tuto", 10);

        foreach (string name in new[] { "Farm", "Snow", "City" })
        {
            LoadingScreenManager.LoadScene(SceneUtility.GetBuildIndexByScenePath("Assets/" + name + ".unity"));
            float deadline = Time.realtimeSinceStartup + 25f;
            while (SceneManager.GetActiveScene().name != name && Time.realtimeSinceStartup < deadline) yield return null;
            Check(SceneManager.GetActiveScene().name == name, "Loading screen reaches " + name);
            yield return new WaitForSecondsRealtime(3);
            yield return Capture(name);
            sheepDestroyer sheep = null;
            float spawnDeadline = Time.realtimeSinceStartup + 8f;
            while (sheep == null && Time.realtimeSinceStartup < spawnDeadline)
            {
                sheep = FindObjectsByType<sheepDestroyer>()
                    .FirstOrDefault(s => (s.CompareTag("sheepy") || s.CompareTag("blacky")) && s.smallCollider != null && !s.caught);
                if (sheep == null) yield return null;
            }
            Check(sheep != null, name + " spawns sheep");
            if (sheep != null)
            {
                int before = gameManager.totalSheepsCaught;
                sheep.sheepClicked();
                Check(gameManager.totalSheepsCaught == before + 1, name + " catch rewards player");
            }
            yield return new WaitForSecondsRealtime(2);
        }
        SceneManager.LoadScene("Alien");
        yield return new WaitForSecondsRealtime(7);
        Check(FindAnyObjectByType<AlienManager>() != null, "Alien scene starts");
        yield return Capture("Alien");
        SceneManager.LoadScene("Credit");
        yield return new WaitForSecondsRealtime(2);
        SceneManager.LoadScene("Start");
        yield return new WaitForSecondsRealtime(2);
        foreach (var text in FindObjectsByType<TMP_Text>())
        {
            text.ForceMeshUpdate();
            if (!string.IsNullOrWhiteSpace(text.text))
                Check(text.textInfo.characterCount > 0, "Text renders: " + text.name);
        }
        Debug.Log("CATCHEEP_SMOKE_COMPLETE errors=" + errors);
        finished = true;
        Application.Quit(errors == 0 ? 0 : 1);
    }

    private void Check(bool condition, string description)
    {
        if (condition) Debug.Log("CATCHEEP_CHECK_OK " + description);
        else Debug.LogError("CATCHEEP_CHECK_FAILED " + description);
    }

    private IEnumerator Capture(string scene)
    {
        if (!Environment.GetCommandLineArgs().Contains("-catcheep-capture")) yield break;
        Directory.CreateDirectory("Logs/screenshots");
        yield return new WaitForEndOfFrame();
        var camera = Camera.main;
        Check(camera != null && camera.enabled, scene + " has an enabled camera");
        if (camera == null) yield break;
        // Render explicitly because a hidden Windows swapchain can return a black backbuffer.
        var canvases = FindObjectsByType<Canvas>().Where(c => c.isRootCanvas && c.renderMode == RenderMode.ScreenSpaceOverlay).ToArray();
        var modes = canvases.Select(c => c.renderMode).ToArray();
        var cameras = canvases.Select(c => c.worldCamera).ToArray();
        var previousTarget = camera.targetTexture;
        var previousActive = RenderTexture.active;
        var target = RenderTexture.GetTemporary(480, 854, 24);
        var pixels = new Texture2D(480, 854, TextureFormat.RGB24, false);
        try
        {
            foreach (var canvas in canvases) { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; }
            Canvas.ForceUpdateCanvases();
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            pixels.ReadPixels(new Rect(0, 0, 480, 854), 0, 0);
            pixels.Apply();
            File.WriteAllBytes(Path.GetFullPath("Logs/screenshots/" + scene + ".png"), pixels.EncodeToPNG());
            Check(pixels.GetPixels32().Any(p => p.r > 20 || p.g > 20 || p.b > 20), scene + " renders visible pixels");
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = modes[i]; canvases[i].worldCamera = cameras[i]; }
            RenderTexture.ReleaseTemporary(target);
            Destroy(pixels);
        }
        yield return null;
    }

    private IEnumerator Watchdog()
    {
        yield return new WaitForSecondsRealtime(120);
        if (!finished) { Debug.LogError("CATCHEEP_SMOKE_TIMEOUT"); Application.Quit(2); }
    }

    private void OnDestroy() { Application.logMessageReceived -= OnLog; }
}
