using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

public sealed class InspectionApp : MonoBehaviour
{
    private string apiUrl = "http://localhost:18765/issues";
    private readonly InspectionState state = new InspectionState();
    private readonly InspectionLookState look = new InspectionLookState();
    private Camera view;
    private CharacterController player;
    private InspectionMarkerSync markerSync;
    private GameObject panel, marker, crosshair;
    private InputField titleInput, descriptionInput;
    private Text message, panelMessage, positionLabel, priorityLabel, controlHint, deviceLabel, syncMessage;
    private Button submitButton, cancelButton;
    private readonly Button[] priorityButtons = new Button[3];
    private Font font;
    private bool submitting;
    private string selectedDeviceId = "", selectedDeviceName = "场景表面";
    private const float MouseSensitivity = 3f;

    [Serializable] private class Position { public float x, y, z; }
    [Serializable] private class Report { public string title, description, priority, deviceId, deviceName; public Position position; }
    [Serializable] private class SavedIssue { public string id; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartApp() { new GameObject("InspectionApp").AddComponent<InspectionApp>(); }

    private void Start()
    {
        font = Font.CreateDynamicFontFromOSFont("Microsoft YaHei", 18);
        if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        view = InspectionScene.Build();
        player = view.gameObject.AddComponent<CharacterController>();
        player.height = 1.6f; player.radius = .3f; player.center = new Vector3(0, -.8f, 0);
        player.skinWidth = .03f; player.stepOffset = .2f;
        BuildUI();
        markerSync = gameObject.AddComponent<InspectionMarkerSync>();
        markerSync.Configure(() => apiUrl, ok => { if (syncMessage != null) syncMessage.text = ok ? "" : "标记同步失败，请检查后端连接。"; });
        look.SetFocus(Application.isFocused);
        look.TryCapture(Time.frameCount);
        ApplyCursorState();
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
    private InputField Field(string name, Transform parent, Vector2 min, Vector2 max, string hint, bool multiline = false)
    {
        var obj = Element(name, parent, min, max); var image = obj.AddComponent<Image>(); image.color = new Color(.94f, .96f, .97f);
        var field = obj.AddComponent<InputField>(); field.targetGraphic = image;
        var width = max.x - min.x; var height = max.y - min.y;
        var text = Label("Text", obj.transform, "", new Vector2(12, -height + 5), new Vector2(width - 12, -5)); text.supportRichText = false; field.textComponent = text;
        var placeholder = Label("Placeholder", obj.transform, hint, new Vector2(12, -height + 5), new Vector2(width - 12, -5)); placeholder.color = new Color(.42f, .48f, .50f); field.placeholder = placeholder;
        if (multiline) { field.lineType = InputField.LineType.MultiLineNewline; text.alignment = TextAnchor.UpperLeft; placeholder.alignment = TextAnchor.UpperLeft; }
        return field;
    }
    private Button MakeButton(string name, Transform parent, string text, Vector2 min, Vector2 max)
    {
        var obj = Element(name, parent, min, max); var image = obj.AddComponent<Image>(); image.color = new Color(.12f, .40f, .42f); var button = obj.AddComponent<Button>(); button.targetGraphic = image;
        var label = Label("Label", obj.transform, text, new Vector2(0, -(max.y-min.y)), new Vector2(max.x-min.x, 0)); label.color = Color.white; label.alignment = TextAnchor.MiddleCenter; return button;
    }

    private void BuildUI()
    {
        if (FindFirstObjectByType<EventSystem>() == null) new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = .5f;

        var header = Element("InspectionHeader", canvas.transform, new Vector2(20, -104), new Vector2(480, -20));
        var headerImage = header.AddComponent<Image>(); headerImage.color = new Color(.07f, .15f, .18f, .88f); headerImage.raycastTarget = false;
        var title = Label("Title", header.transform, "园区机房巡检", new Vector2(18, -40), new Vector2(440, -8), 23); title.color = Color.white;
        var hint = Label("Hint", header.transform, "点击设备或场景表面，标记需要处理的问题", new Vector2(18, -72), new Vector2(440, -42), 16); hint.color = new Color(.79f, .88f, .87f);

        var controls = Element("Controls", canvas.transform, new Vector2(20, -56), new Vector2(570, -16));
        Rect(controls, new Vector2(0, 0), new Vector2(0, 0), new Vector2(20, 16), new Vector2(570, 56));
        var controlsImage = controls.AddComponent<Image>(); controlsImage.color = new Color(.07f, .15f, .18f, .88f); controlsImage.raycastTarget = false;
        controlHint = Label("Help", controls.transform, "WASD 移动   ·   鼠标观察   ·   左键标记   ·   Esc 释放鼠标", new Vector2(14, -36), new Vector2(535, -4), 16); controlHint.color = Color.white;
        message = Label("Message", canvas.transform, "", new Vector2(20, -150), new Vector2(560, -110), 17); message.color = Color.white;
        syncMessage = Label("SyncMessage", canvas.transform, "", new Vector2(20, -190), new Vector2(600, -150), 16); syncMessage.color = new Color(1f, .78f, .30f);

        crosshair = new GameObject("Crosshair", typeof(RectTransform)); crosshair.transform.SetParent(canvas.transform, false);
        Rect(crosshair, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-10, -10), new Vector2(10, 10));
        CrosshairBar(crosshair.transform, new Vector2(-1, -8), new Vector2(1, 8));
        CrosshairBar(crosshair.transform, new Vector2(-8, -1), new Vector2(8, 1));

        panel = new GameObject("ModalOverlay", typeof(RectTransform), typeof(Image)); panel.transform.SetParent(canvas.transform, false);
        Rect(panel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero); panel.GetComponent<Image>().color = new Color(.03f, .08f, .10f, .68f);
        var card = new GameObject("ReportPanel", typeof(RectTransform), typeof(Image)); card.transform.SetParent(panel.transform, false);
        Rect(card, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(-260, -300), new Vector2(260, 300)); card.GetComponent<Image>().color = new Color(.98f, .99f, .99f);
        Label("Heading", card.transform, "上报巡检问题", new Vector2(28, -58), new Vector2(492, -20), 24);
        deviceLabel = Label("Device", card.transform, "", new Vector2(28, -88), new Vector2(492, -58), 16); deviceLabel.color = new Color(.36f, .43f, .45f);
        Label("Title label", card.transform, "问题标题  *", new Vector2(28, -128), new Vector2(492, -98));
        titleInput = Field("Title", card.transform, new Vector2(28, -174), new Vector2(492, -132), "例如：A-01 机柜温度异常");
        Label("Description label", card.transform, "问题描述（选填）", new Vector2(28, -213), new Vector2(492, -183));
        descriptionInput = Field("Description", card.transform, new Vector2(28, -286), new Vector2(492, -218), "例如：机柜右侧温度明显偏高", true);
        Label("Priority label", card.transform, "优先级", new Vector2(28, -324), new Vector2(160, -294));
        priorityLabel = Label("Priority value", card.transform, "", new Vector2(172, -324), new Vector2(420, -294), 16);
        var priorities = new[] { "低", "中", "高" };
        for (int i = 0; i < priorities.Length; i++) { int captured = i; priorityButtons[i] = MakeButton("Priority " + i, card.transform, priorities[i], new Vector2(28 + i * 110, -369), new Vector2(124 + i * 110, -332)); priorityButtons[i].onClick.AddListener(() => SelectPriority(captured)); }
        Label("Position label", card.transform, "标记位置（场景坐标）", new Vector2(28, -407), new Vector2(492, -379), 16);
        positionLabel = Label("Position", card.transform, "", new Vector2(28, -436), new Vector2(492, -407), 16);
        panelMessage = Label("Panel message", card.transform, "", new Vector2(28, -486), new Vector2(492, -442), 16); panelMessage.color = new Color(.62f, .18f, .16f);
        cancelButton = MakeButton("Cancel", card.transform, "取消", new Vector2(28, -554), new Vector2(232, -505)); cancelButton.onClick.AddListener(Cancel);
        submitButton = MakeButton("Submit", card.transform, "提交问题", new Vector2(256, -554), new Vector2(492, -505)); submitButton.onClick.AddListener(Submit);
        panel.SetActive(false);
    }
    private int selectedPriority = 1;

    private static void CrosshairBar(Transform parent, Vector2 min, Vector2 max)
    {
        var bar = new GameObject("CrosshairBar", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        bar.transform.SetParent(parent, false);
        Rect(bar, new Vector2(.5f, .5f), new Vector2(.5f, .5f), min, max);
        var graphic = bar.GetComponent<UnityEngine.UI.Image>(); graphic.color = Color.white; graphic.raycastTarget = false;
        var outline = bar.AddComponent<Outline>(); outline.effectColor = new Color(0, 0, 0, .85f); outline.effectDistance = Vector2.one;
    }

    private void ApplyCursorState()
    {
        Cursor.lockState = look.CanLook ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !look.CanLook;
        if (crosshair != null) crosshair.SetActive(look.CanLook);
        if (controlHint != null) controlHint.text = look.CanLook
            ? "WASD 移动   ·   鼠标观察   ·   左键标记   ·   Esc 释放鼠标"
            : "点击画面恢复巡检   ·   Esc 释放鼠标";
    }

    private void OnApplicationFocus(bool focused)
    {
        look.SetFocus(focused);
        ApplyCursorState();
    }

    private void OnDestroy()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void SelectPriority(int priority)
    {
        selectedPriority = priority;
        var labels = new[] { "低", "中", "高" };
        priorityLabel.text = "当前选择：" + labels[priority];
        for (int i = 0; i < priorityButtons.Length; i++)
            priorityButtons[i].GetComponent<Image>().color = i == priority ? new Color(.12f, .40f, .42f) : new Color(.36f, .43f, .45f);
    }

    private void Update()
    {
        if (state.IsOpen) return;
        if (Input.GetKeyDown(KeyCode.Escape)) { look.Release(); ApplyCursorState(); return; }
        if (!look.CanLook)
        {
            if (Input.GetMouseButtonDown(0) && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()) && look.TryCapture(Time.frameCount)) ApplyCursorState();
            return;
        }
        look.ApplyMouseDelta(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"), MouseSensitivity);
        view.transform.rotation = Quaternion.Euler(look.Pitch, look.Yaw, 0);
        var move = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        var direction = Quaternion.Euler(0, view.transform.eulerAngles.y, 0) * move.normalized; player.SimpleMove(direction * 4f);
        if (!look.CanMark(Time.frameCount) || !Input.GetMouseButtonDown(0) || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())) return;
        if (!Physics.Raycast(view.ViewportPointToRay(new Vector3(.5f, .5f, 0)), out var hit, 100f)) return;
        BeginReport(hit);
    }

    private void BeginReport(RaycastHit hit)
    {
        if (!state.TryOpen()) return;
        if (!look.OpenModal()) { state.Close(); return; }
        var device = hit.collider.GetComponentInParent<InspectionDevice>();
        selectedDeviceId = device != null ? device.DeviceID : "";
        selectedDeviceName = device != null ? device.DeviceName : "场景表面";
        deviceLabel.text = "标记对象：" + selectedDeviceName;
        marker = GameObject.CreatePrimitive(PrimitiveType.Sphere); marker.name = "待提交标记"; marker.transform.position = hit.point; marker.transform.localScale = Vector3.one * .18f; marker.GetComponent<Renderer>().material.color = Color.red; Destroy(marker.GetComponent<Collider>());
        titleInput.text = ""; descriptionInput.text = ""; panelMessage.text = ""; SelectPriority(1);
        positionLabel.text = string.Format(System.Globalization.CultureInfo.InvariantCulture, "X {0:F2}   Y {1:F2}   Z {2:F2}", hit.point.x, hit.point.y, hit.point.z);
        panel.SetActive(true); ApplyCursorState(); titleInput.ActivateInputField();
    }

    private void Cancel() { if (submitting) return; if (marker != null) Destroy(marker); marker = null; panel.SetActive(false); state.Close(); look.CloseModal(Time.frameCount); ApplyCursorState(); message.text = "已取消上报。"; }
    private void Submit()
    {
        if (submitting) return;
        if (!InspectionState.ValidTitle(titleInput.text)) { panelMessage.text = "请填写标题。"; return; }
        var p = marker.transform.position;
        var report = new Report { title = titleInput.text.Trim(), description = descriptionInput.text, priority = new[] { "low", "medium", "high" }[selectedPriority], deviceId = selectedDeviceId, deviceName = selectedDeviceName, position = new Position { x = p.x, y = p.y, z = p.z } };
        StartCoroutine(Send(report));
    }
    private IEnumerator Send(Report report)
    {
        submitting = true; submitButton.interactable = false; cancelButton.interactable = false; titleInput.interactable = false; descriptionInput.interactable = false;
        foreach (var button in priorityButtons) button.interactable = false;
        panelMessage.text = "正在提交…";
        using (var request = new UnityWebRequest(apiUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(report))); request.downloadHandler = new DownloadHandlerBuffer(); request.SetRequestHeader("Content-Type", "application/json"); request.timeout = 10;
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                SavedIssue saved = null;
                try { saved = JsonUtility.FromJson<SavedIssue>(request.downloadHandler.text); } catch (ArgumentException) { }
                if (saved != null && !string.IsNullOrEmpty(saved.id))
                {
                    marker.name = "问题标记 " + saved.id;
                    marker.GetComponent<Renderer>().material.color = new Color(.08f, .63f, .56f);
                    markerSync.Register(saved.id, marker);
                    message.text = "问题已提交。";
                }
                else
                {
                    Destroy(marker);
                    message.text = "问题已提交，标记将在同步后恢复。";
                }
                marker = null; panel.SetActive(false); state.Close(); look.CloseModal(Time.frameCount); ApplyCursorState();
            }
            else { panelMessage.text = "提交失败，请检查后端服务后重试。"; }
        }
        submitting = false; submitButton.interactable = true; cancelButton.interactable = true; titleInput.interactable = true; descriptionInput.interactable = true;
        foreach (var button in priorityButtons) button.interactable = true;
    }
}
