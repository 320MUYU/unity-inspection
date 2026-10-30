using UnityEngine;

public sealed class InspectionDevice : MonoBehaviour
{
    public string DeviceID { get; private set; }
    public string DeviceName { get; private set; }

    public void Initialize(string id, string displayName)
    {
        DeviceID = id;
        DeviceName = displayName;
    }
}
