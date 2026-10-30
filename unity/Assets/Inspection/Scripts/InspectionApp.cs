using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

public sealed class InspectionApp : MonoBehaviour
{
    private const string Api = "http://localhost:18765/issues";
    private readonly InspectionState state = new InspectionState();
    private Camera view;
    private GameObject panel, marker;
    private InputField titleInput, descriptionInput;
    private Dropdown priorityInput;
    private Text message, panelMessage;
    private Button submitButton, cancelButton;
    private Font font;
    private bool submitting;
    private float pitch;

    [Serializable] private class Position { public float x, y, z; }
    [Serializable] private class Report { public string title, description, priority; public Position position; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartApp() { new GameObject("InspectionApp").AddComponent<InspectionApp>(); }

    private void Start()
    {
        font = Font.CreateDynamicFontFromOSFont("Microsoft YaHei", 18);
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildScene();
        BuildUI();
    }

    private static GameObject Block(string name, Vector3 position, Vector3 scale, Color color)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name; obj.transform.position = position; obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().material.color = color;
        return obj;
    }

    private void BuildScene()
    {
        Block("地面", new Vector3(0, -0.1f, 4), new Vector3(14, 0.2f, 16), new Color(.55f, .60f, .62f));
        Block("后墙", new Vector3(0, 2, 11.5f), new Vector3(14, 4, .2f), new Color(.77f, .81f, .82f));
        Block("左墙", new Vector3(-7, 2, 4), new Vector3(.2f, 4, 15), new Color(.72f, .77f, .79f));
        Block("一号机柜", new Vector3(-2, 1, 5), new Vector3(1.5f, 2, 1), new Color(.18f, .40f, .45f));
        Block("二号设备", new Vector3(2, .75f, 7), new Vector3(2, 1.5f, 1.5f), new Color(.35f, .49f, .53f));
        var lightObject = new GameObject("Directional Light"); var light = lightObject.AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.2f; lightObject.transform.rotation = Quaternion.Euler(50, -30, 0);
        var cameraObject = new GameObject("Inspection Camera"); cameraObject.tag = "MainCamera";
        view = cameraObject.AddComponent<Camera>();
        view.transform.position = new Vector3(0, 1.6f, 0); view.transform.rotation = Quaternion.identity;
        view.clearFlags = CameraClearFlags.SolidColor; view.backgroundColor = new Color(.63f, .76f, .8f);
    }

    private static RectTransform Rect(GameObject obj, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var r = obj.GetComponent<RectTransform>(); r.anchorMin = anchorMin; r.anchorMax = anchorMax; r.offsetMin = offsetMin; r.offsetMax = offsetMax; return r;
    }
    private GameObject Element(string name, Transform parent, Vector2 min, Vector2 max)
    {
        var obj = new GameObject(name, typeof(RectTransform)); obj.transform.SetParent(parent, false);
        Rect(obj, new Vector2(0, 1), new Vector2(0, 1), min, max); return obj;
    }
    private Text Label(string name, Transform parent, string value, Vector2 min, Vector2 max, int size = 18)
    {
        var obj = Element(name, parent, min, max); var text = obj.AddComponent<Text>(); text.font = font; text.fontSize = size; text.color = new Color(.12f, .2f, .25f); text.text = value; text.alignment = TextAnchor.MiddleLeft; text.raycastTarget = false; return text;
    }
    private InputField Field(string name, Transform parent, Vector2 min, Vector2 max)
    {
        var obj = Element(name, parent, min, max); var image = obj.AddComponent<Image>(); image.color = new Color(.94f, .96f, .97f);
        var field = obj.AddComponent<InputField>(); var text = Label("Text", obj.transform, "", new Vector2(8, -34), new Vector2(max.x - min.x - 8, -2)); text.supportRichText = false; field.textComponent = text;
        var placeholder = Label("Placeholder", obj.transform, "", new Vector2(8, -34), new Vector2(max.x - min.x - 8, -2)); placeholder.color = Color.gray; field.placeholder = placeholder; return field;
    }
    private Button MakeButton(string name, Transform parent, string text, Vector2 min, Vector2 max)
    {
        var obj = Element(name, parent, min, max); var image = obj.AddComponent<Image>(); image.color = new Color(.12f, .48f, .48f); var button = obj.AddComponent<Button>();
        var label = Label("Label", obj.transform, text, new Vector2(0, -(max.y-min.y)), new Vector2(max.x-min.x, 0)); label.color = Color.white; label.alignment = TextAnchor.MiddleCenter; return button;
    }

    private void BuildUI()
    {
        var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280, 720);
        var help = Label("Help", canvas.transform, "WASD 移动  |  按住右键转动视角  |  左键点击设备或场景标记", new Vector2(20, -54), new Vector2(900, -12)); help.color = Color.white;
        message = Label("Message", canvas.transform, "", new Vector2(20, -94), new Vector2(900, -58)); message.color = Color.white;
        panel = new GameObject("Report panel", typeof(RectTransform), typeof(Image)); panel.transform.SetParent(canvas.transform, false);
        Rect(panel, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-240, -240), new Vector2(240, 240)); panel.GetComponent<Image>().color = new Color(.97f, .98f, .98f);
        Label("Heading", panel.transform, "上报巡检问题", new Vector2(24, -58), new Vector2(456, -20), 24);
        Label("Title label", panel.transform, "标题（必填）", new Vector2(24, -100), new Vector2(456, -70));
        titleInput = Field("Title", panel.transform, new Vector2(24, -140), new Vector2(456, -104));
        Label("Description label", panel.transform, "描述（可选）", new Vector2(24, -180), new Vector2(456, -150));
        descriptionInput = Field("Description", panel.transform, new Vector2(24, -220), new Vector2(456, -184));
        Label("Priority label", panel.transform, "优先级", new Vector2(24, -260), new Vector2(160, -230));
        // Three explicit buttons keep the priority choice usable without a complex dropdown template.
        var priorities = new[] { "低", "中", "高" }; var priorityValue = 1;
        var priorityLabel = Label("Priority value", panel.transform, "当前：中", new Vector2(170, -260), new Vector2(300, -230));
        for (int i = 0; i < 3; i++) { int captured = i; var button = MakeButton("Priority " + i, panel.transform, priorities[i], new Vector2(24 + i * 95, -305), new Vector2(104 + i * 95, -270)); button.onClick.AddListener(() => { priorityValue = captured; selectedPriority = priorityValue; priorityLabel.text = "当前：" + priorities[captured]; }); }
        panelMessage = Label("Panel message", panel.transform, "", new Vector2(24, -352), new Vector2(456, -315), 16);
        submitButton = MakeButton("Submit", panel.transform, "提交", new Vector2(24, -410), new Vector2(200, -365)); submitButton.onClick.AddListener(Submit);
        cancelButton = MakeButton("Cancel", panel.transform, "取消", new Vector2(220, -410), new Vector2(396, -365)); cancelButton.onClick.AddListener(Cancel);
        panel.SetActive(false);
    }
    private int selectedPriority = 1;

    private void Update()
    {
        if (state.IsOpen) return;
        if (Input.GetMouseButton(1)) { float yaw = Input.GetAxis("Mouse X") * 3; pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * 3, -80, 80); view.transform.rotation = Quaternion.Euler(pitch, view.transform.eulerAngles.y + yaw, 0); }
        var move = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        var direction = Quaternion.Euler(0, view.transform.eulerAngles.y, 0) * move.normalized; view.transform.position += direction * (4 * Time.deltaTime);
        if (!Input.GetMouseButtonDown(0) || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())) return;
        if (!Physics.Raycast(view.ScreenPointToRay(Input.mousePosition), out var hit, 100f)) return;
        if (!state.TryOpen()) return;
        marker = GameObject.CreatePrimitive(PrimitiveType.Sphere); marker.name = "待提交标记"; marker.transform.position = hit.point; marker.transform.localScale = Vector3.one * .18f; marker.GetComponent<Renderer>().material.color = Color.red; Destroy(marker.GetComponent<Collider>());
        titleInput.text = ""; descriptionInput.text = ""; panelMessage.text = ""; panel.SetActive(true);
    }

    private void Cancel() { if (submitting) return; if (marker != null) Destroy(marker); marker = null; panel.SetActive(false); state.Close(); message.text = "已取消上报。"; }
    private void Submit()
    {
        if (submitting) return;
        if (!InspectionState.ValidTitle(titleInput.text)) { panelMessage.text = "请填写标题。"; return; }
        var p = marker.transform.position;
        var report = new Report { title = titleInput.text.Trim(), description = descriptionInput.text, priority = new[] { "low", "medium", "high" }[selectedPriority], position = new Position { x = p.x, y = p.y, z = p.z } };
        StartCoroutine(Send(report));
    }
    private IEnumerator Send(Report report)
    {
        submitting = true; submitButton.interactable = false; cancelButton.interactable = false; panelMessage.text = "正在提交…";
        using (var request = new UnityWebRequest(Api, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(report))); request.downloadHandler = new DownloadHandlerBuffer(); request.SetRequestHeader("Content-Type", "application/json"); request.timeout = 10;
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success) { marker.name = "已提交标记"; marker = null; panel.SetActive(false); state.Close(); message.text = "问题已提交。"; }
            else { panelMessage.text = "提交失败，请检查后端服务后重试。"; }
        }
        submitting = false; submitButton.interactable = true; cancelButton.interactable = true;
    }
}
