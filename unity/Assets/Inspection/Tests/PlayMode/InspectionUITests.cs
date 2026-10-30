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
    public IEnumerator PlayerCannotMoveThroughTheCabinet()
    {
        yield return null;
        var app = Object.FindFirstObjectByType<InspectionApp>();
        var camera = Field<Camera>(app, "view");
        var controller = Field<CharacterController>(app, "player");
        var originalPosition = camera.transform.position;
        try
        {
            camera.transform.position = new Vector3(0, 1.6f, 5.6f);
            Physics.SyncTransforms();
            var flags = controller.Move(Vector3.left * 8f);
            Assert.IsTrue((flags & CollisionFlags.Sides) != 0, "The rack should block horizontal movement.");
            Assert.Greater(camera.transform.position.x, -2.0f, "The player must remain outside the rack.");
        }
        finally { camera.transform.position = originalPosition; }
    }

    [UnityTest]
    public IEnumerator CentreAimRayHitsTheCabinetInFrontOfThePlayer()
    {
        yield return null;
        var app = Object.FindFirstObjectByType<InspectionApp>();
        var camera = Field<Camera>(app, "view");
        var originalRotation = camera.transform.rotation;
        try
        {
            camera.transform.LookAt(new Vector3(-2.8f, 1.1f, 5.6f));
            Physics.SyncTransforms();
            var ray = camera.ViewportPointToRay(new Vector3(.5f, .5f, 0));
            Assert.IsTrue(Physics.Raycast(ray, out var hit, 20f));
            StringAssert.Contains("A-01", hit.collider.gameObject.name);
        }
        finally { camera.transform.rotation = originalRotation; }
    }

    [UnityTest]
    public IEnumerator ReportFormValidatesTitleAndCancelRemovesPendingMarker()
    {
        yield return null;
        var app = Object.FindFirstObjectByType<InspectionApp>();
        Assert.IsNotNull(app, "The inspection app should start with the scene.");
        var panel = Field<GameObject>(app, "panel");
        var state = Field<InspectionState>(app, "state");
        var look = Field<InspectionLookState>(app, "look");
        Assert.IsFalse(panel.activeSelf);
        look.SetFocus(true);
        if (!look.IsCaptured) look.TryCapture(Time.frameCount);
        Assert.IsTrue(look.OpenModal());
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
        Assert.IsTrue(look.IsCaptured, "Closing the form should restore inspection control.");
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
        var crosshair = Field<GameObject>(app, "crosshair");
        var controlHint = Field<Text>(app, "controlHint");
        var previousHint = controlHint.text;
        var originalPosition = camera.transform.position;
        var originalRotation = camera.transform.rotation;
        var previousCrosshair = crosshair.activeSelf;
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
            controlHint.text = "WASD 移动   ·   鼠标观察   ·   左键标记   ·   Esc 释放鼠标";
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 0.5f;
            camera.targetTexture = target;
            for (var i = 0; i < 3; i++)
            {
                panel.SetActive(i == 1);
                crosshair.SetActive(i != 1);
                if (i == 2)
                {
                    camera.transform.position = new Vector3(-3.9f, 1.4f, 9.2f);
                    camera.transform.LookAt(new Vector3(-5.6f, 1.25f, 11.6f));
                }
                Canvas.ForceUpdateCanvases();
                camera.Render();
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                image.Apply();
                var filename = i == 0 ? "room-review.png" : i == 1 ? "form-review.png" : "electrical-box-review.png";
                File.WriteAllBytes(Path.Combine(directory, filename), image.EncodeToPNG());
            }
        }
        finally
        {
            panel.SetActive(false);
            crosshair.SetActive(previousCrosshair);
            controlHint.text = previousHint;
            camera.transform.position = originalPosition;
            camera.transform.rotation = originalRotation;
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
        var look = Field<InspectionLookState>(app, "look");
        look.SetFocus(true);
        if (!look.IsCaptured) look.TryCapture(Time.frameCount);
        Assert.IsTrue(look.OpenModal());
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
        Assert.IsTrue(look.IsCaptured, "Successful reporting should restore inspection control.");
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
