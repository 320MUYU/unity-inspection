using NUnit.Framework;
using UnityEngine;

public class InspectionDeviceTests
{
    [Test]
    public void DevicePartsResolveToTheirOwnStableDevice()
    {
        var camera = InspectionScene.Build();
        try
        {
            foreach (var example in new[] {
                new[] { "Cabinet A-01", "A-01", "机柜 A-01" },
                new[] { "A-01 vent 1", "A-01", "机柜 A-01" },
                new[] { "A-02 door", "A-02", "机柜 A-02" },
                new[] { "Electrical switch 1", "POWER-01", "配电箱 POWER-01" }
            })
            {
                var part = GameObject.Find(example[0]);
                Assert.IsNotNull(part, example[0]);
                var device = part.GetComponentInParent<InspectionDevice>();
                Assert.IsNotNull(device, example[0]);
                Assert.AreEqual(example[1], device.DeviceID);
                Assert.AreEqual(example[2], device.DeviceName);
            }
            Assert.IsNull(GameObject.Find("Ground").GetComponentInParent<InspectionDevice>());
        }
        finally { Object.DestroyImmediate(camera.transform.parent.gameObject); }
    }
}
