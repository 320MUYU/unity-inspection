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
        EnsureRuntimeShaders();
        if (!File.Exists(ScenePath))
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
        }
    }

    private static void EnsureRuntimeShaders()
    {
        const string folder = "Assets/Inspection/Resources";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Inspection", "Resources");
        CreateShaderReference(folder, "AlwaysIncludeStandard", "Standard");
        CreateShaderReference(folder, "AlwaysIncludeUI", "UI/Default");
        CreateShaderReference(folder, "AlwaysIncludeText", "GUI/Text Shader");
        AssetDatabase.SaveAssets();
    }

    private static void CreateShaderReference(string folder, string name, string shaderName)
    {
        var path = folder + "/" + name + ".mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
        var shader = Shader.Find(shaderName);
        if (shader == null) { Debug.LogError("Required shader not found: " + shaderName); return; }
        AssetDatabase.CreateAsset(new Material(shader), path);
    }

    [MenuItem("Inspection/Open inspection scene")]
    public static void OpenScene()
    {
        EnsureScene();
        EditorSceneManager.OpenScene(ScenePath);
    }
}
