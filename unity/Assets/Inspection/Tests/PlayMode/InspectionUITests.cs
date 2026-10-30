using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.Networking;

public class InspectionUITests
{
    private static T Field<T>(InspectionApp app, string name)
    {
        return (T)typeof(InspectionApp).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(app);
    }

    [UnityTest]
    public IEnumerator ReportFormValidatesTitleAndCancelRemovesPendingMarker()
    {
        yield return null;
        var app = Object.FindFirstObjectByType<InspectionApp>();
        Assert.IsNotNull(app, "The inspection app should start with the scene.");
        var panel = Field<GameObject>(app, "panel");
        var state = Field<InspectionState>(app, "state");
        Assert.IsFalse(panel.activeSelf);
        Assert.IsTrue(state.TryOpen());
        var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        typeof(InspectionApp).GetField("marker", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(app, marker);
        panel.SetActive(true);
        Field<InputField>(app, "titleInput").text = " ";
        Field<Button>(app, "submitButton").onClick.Invoke();
        StringAssert.Contains("填写标题", Field<Text>(app, "panelMessage").text);
        Assert.IsTrue(panel.activeSelf);
        Field<Button>(app, "cancelButton").onClick.Invoke();
        yield return null;
        Assert.IsFalse(panel.activeSelf);
        Assert.IsFalse(state.IsOpen);
        Assert.IsTrue(marker == null, "Cancelling must remove the unsaved marker.");
    }

    [UnityTest]
    public IEnumerator CaptureRoomAndReportFormForVisualReview()
    {
        yield return null;
        var app = Object.FindFirstObjectByType<InspectionApp>();
        var camera = Field<Camera>(app, "view");
        var panel = Field<GameObject>(app, "panel");
        var canvas = Object.FindFirstObjectByType<Canvas>();
        var previousMode = canvas.renderMode;
        var previousCamera = canvas.worldCamera;
        var target = new RenderTexture(1280, 720, 24);
        var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        var directory = Path.Combine(Application.dataPath, "../Logs");
        Directory.CreateDirectory(directory);
        try
        {
            typeof(InspectionApp).GetMethod("SelectPriority", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(app, new object[] { 1 });
            Field<Text>(app, "positionLabel").text = "X -2.80   Y 1.10   Z 5.03";
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 0.5f;
            camera.targetTexture = target;
            for (var i = 0; i < 2; i++)
            {
                panel.SetActive(i == 1);
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                image.Apply();
                File.WriteAllBytes(Path.Combine(directory, i == 0 ? "room-review.png" : "form-review.png"), image.EncodeToPNG());
            }
        }
        finally
        {
            panel.SetActive(false);
            camera.targetTexture = null;
            RenderTexture.active = null;
            canvas.renderMode = previousMode;
            canvas.worldCamera = previousCamera;
            Object.Destroy(target);
            Object.Destroy(image);
        }
        yield return null;
    }

    [UnityTest]
    public IEnumerator ReportFormSendsToIsolatedBackend()
    {
        if (System.Environment.GetEnvironmentVariable("INSPECTION_RUN_INTEGRATION") != "1")
            Assert.Ignore("Set INSPECTION_RUN_INTEGRATION=1 and start the isolated test backend on port 18766.");
        yield return null;
        var app = Object.FindFirstObjectByType<InspectionApp>();
        var panel = Field<GameObject>(app, "panel");
        var state = Field<InspectionState>(app, "state");
        Assert.IsTrue(state.TryOpen());
        var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        marker.transform.position = new Vector3(-2.8f, 1.1f, 5.03f);
        typeof(InspectionApp).GetField("marker", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(app, marker);
        typeof(InspectionApp).GetField("apiUrl", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(app, "http://localhost:18766/issues");
        panel.SetActive(true);
        Field<InputField>(app, "titleInput").text = "Unity integration test";
        typeof(InspectionApp).GetMethod("SelectPriority", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(app, new object[] { 2 });
        Field<Button>(app, "submitButton").onClick.Invoke();
        var deadline = Time.realtimeSinceStartup + 15f;
        while (state.IsOpen && Time.realtimeSinceStartup < deadline) yield return null;
        Assert.IsFalse(state.IsOpen, Field<Text>(app, "panelMessage").text);
        using (var request = UnityWebRequest.Get("http://localhost:18766/issues"))
        {
            yield return request.SendWebRequest();
            Assert.AreEqual(UnityWebRequest.Result.Success, request.result, request.error);
            StringAssert.Contains("Unity integration test", request.downloadHandler.text);
            StringAssert.Contains("\"priority\":\"high\"", request.downloadHandler.text);
            StringAssert.Contains("\"status\":\"open\"", request.downloadHandler.text);
        }
    }
}
