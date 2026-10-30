using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public sealed class InspectionPosition { public float x, y, z; }

[Serializable]
public sealed class InspectionIssue
{
    public string id, title, status, deviceId, deviceName;
    public InspectionPosition position;
}

[Serializable]
internal sealed class InspectionIssueList { public InspectionIssue[] items; }

public sealed class InspectionMarkerSync : MonoBehaviour
{
    private readonly Dictionary<string, GameObject> markers = new Dictionary<string, GameObject>();
    private Func<string> apiUrl;
    private Action<bool> onSyncState;
    private int revision;

    public void Configure(Func<string> url, Action<bool> stateChanged)
    {
        apiUrl = url;
        onSyncState = stateChanged;
        StartCoroutine(Synchronize());
    }

    public GameObject GetMarker(string id) { return markers.TryGetValue(id, out var marker) ? marker : null; }

    public void Register(string id, GameObject marker)
    {
        if (string.IsNullOrEmpty(id) || marker == null) throw new ArgumentException("A saved marker requires an issue ID.");
        if (markers.TryGetValue(id, out var previous) && previous != marker) Destroy(previous);
        markers[id] = marker;
        revision++;
    }

    public void Reconcile(InspectionIssue[] issues)
    {
        var visible = new HashSet<string>();
        foreach (var issue in issues)
        {
            if (issue.status == "resolved") continue;
            visible.Add(issue.id);
            if (!markers.TryGetValue(issue.id, out var marker) || marker == null)
            {
                marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                marker.name = "问题标记 " + issue.id;
                marker.transform.localScale = Vector3.one * .18f;
                marker.GetComponent<Renderer>().material.color = new Color(.08f, .63f, .56f);
                Destroy(marker.GetComponent<Collider>());
                markers[issue.id] = marker;
            }
            marker.transform.position = new Vector3(issue.position.x, issue.position.y, issue.position.z);
        }
        var remove = new List<string>();
        foreach (var id in markers.Keys) if (!visible.Contains(id)) remove.Add(id);
        foreach (var id in remove) { Destroy(markers[id]); markers.Remove(id); }
    }

    private static bool TryParse(string json, out InspectionIssue[] issues)
    {
        issues = null;
        if (string.IsNullOrWhiteSpace(json)) return false;
        var trimmed = json.Trim();
        if (!trimmed.StartsWith("[") || !trimmed.EndsWith("]")) return false;
        try
        {
            var result = JsonUtility.FromJson<InspectionIssueList>("{\"items\":" + trimmed + "}");
            if (result == null || result.items == null) return false;
            foreach (var item in result.items)
                if (item == null || string.IsNullOrEmpty(item.id) ||
                    (item.status != "open" && item.status != "in_progress" && item.status != "resolved") ||
                    (item.status != "resolved" && item.position == null)) return false;
            issues = result.items;
            return true;
        }
        catch (ArgumentException) { return false; }
    }

    private IEnumerator Synchronize()
    {
        while (true)
        {
            var requestRevision = revision;
            using (var request = UnityWebRequest.Get(apiUrl()))
            {
                request.timeout = 10;
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success || !TryParse(request.downloadHandler.text, out var items))
                    onSyncState?.Invoke(false);
                else if (requestRevision == revision)
                {
                    Reconcile(items);
                    onSyncState?.Invoke(true);
                }
            }
            yield return new WaitForSecondsRealtime(3f);
        }
    }

    private void OnDestroy()
    {
        foreach (var marker in markers.Values) if (marker != null) Destroy(marker);
        markers.Clear();
    }
}
