using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class InspectionProjectSetup
{
    private const string ScenePath = "Assets/Inspection/Inspection.unity";

    static InspectionProjectSetup() { EditorApplication.delayCall += EnsureScene; }

    private static void EnsureScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!File.Exists(ScenePath))
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
        }
    }

    [MenuItem("Inspection/Open inspection scene")]
    public static void OpenScene()
    {
        EnsureScene();
        EditorSceneManager.OpenScene(ScenePath);
    }
}
