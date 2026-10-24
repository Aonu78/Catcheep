using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CatcheepCompatibility
{
    [MenuItem("Catcheep/Validate Unity 6 Migration")]
    public static void Validate()
    {
        int missing = 0;
        int texts = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:TMP_FontAsset", new[] { "Assets" }))
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
            font.ReadFontAssetDefinition();
            EditorUtility.SetDirty(font);
        }
        foreach (var guid in AssetDatabase.FindAssets("t:TMP_SpriteAsset", new[] { "Assets" }))
        {
            var sprite = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(AssetDatabase.GUIDToAssetPath(guid));
            sprite.UpdateLookupTables();
            EditorUtility.SetDirty(sprite);
        }
        AssetDatabase.SaveAssets();
        foreach (var buildScene in EditorBuildSettings.scenes.Where(s => s.enabled))
        {
            var scene = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Single);
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                {
                    int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
                    if (count > 0) Debug.LogError(buildScene.path + ": missing script on " + transform.name);
                    missing += count;
                }
                foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    texts++;
                    if (text.font == null) throw new Exception(buildScene.path + ": missing font on " + text.name);
                    text.ForceMeshUpdate(true, true);
                }
            }
            Debug.Log("CATCHEEP_SCENE_OK " + buildScene.path);
            EditorSceneManager.SaveScene(scene);
        }
        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            foreach (var transform in prefab.GetComponentsInChildren<Transform>(true))
            {
                int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject);
                if (count > 0) Debug.LogError(path + ": missing script on " + transform.name);
                missing += count;
            }
        }
        EditorSceneManager.OpenScene("Assets/Opening.unity");
        if (missing > 0) throw new Exception("Missing script references: " + missing);
        Debug.Log("CATCHEEP_VALIDATION_PASSED: 9 scenes, " + texts + " text components, all game prefabs.");
    }

    public static void BuildWindowsSmokeTest()
    {
        Validate();
        string company = PlayerSettings.companyName;
        string product = PlayerSettings.productName;
        try
        {
            // Isolate automated playthroughs from the player's existing save data.
            PlayerSettings.companyName = "CatcheepQA";
            PlayerSettings.productName = "Unity6Compatibility";
            Build("Builds/WindowsSmoke/Catcheep.exe", BuildTarget.StandaloneWindows64);
        }
        finally
        {
            PlayerSettings.companyName = company;
            PlayerSettings.productName = product;
            AssetDatabase.SaveAssets();
        }
    }

    public static void BuildAndroid()
    {
        GooglePlayGames.Editor.GPGSUtil.GenerateAndroidManifest();
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        Build("Builds/Android/Catcheep.apk", BuildTarget.Android);
    }

    private static void Build(string path, BuildTarget target)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
            locationPathName = path,
            target = target,
            options = BuildOptions.Development
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("Build failed: " + report.summary.result);
        Debug.Log("CATCHEEP_BUILD_PASSED " + path);
    }
}
