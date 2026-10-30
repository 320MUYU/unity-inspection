using NUnit.Framework;
using UnityEngine;

public class InspectionEquipmentAppearanceTests
{
    [Test]
    public void ElectricalBoxIsVisuallyDistinctAndCanBeInspected()
    {
        var camera = InspectionScene.Build();
        try
        {
            var rack = GameObject.Find("Cabinet A-01");
            var box = GameObject.Find("Electrical cabinet");
            Assert.IsNotNull(rack);
            Assert.IsNotNull(box);
            Physics.SyncTransforms();
            var rackBounds = rack.GetComponent<Collider>().bounds;
            var boxBounds = box.GetComponent<Collider>().bounds;
            Assert.Less(boxBounds.size.y, rackBounds.size.y * .8f, "The electrical box should be smaller than the equipment rack.");
            Assert.Greater(boxBounds.min.y, .3f, "The electrical box should be raised off the floor.");
            Assert.IsNotNull(GameObject.Find("Electrical warning triangle"));
            Assert.IsNotNull(GameObject.Find("Electrical warning bolt"));
            Assert.IsNotNull(GameObject.Find("Electrical conduit 1"));
            Assert.IsNotNull(GameObject.Find("Electrical switch 1"));

            var origin = new Vector3(-4.2f, 1.25f, 9.9f);
            var direction = new Vector3(-5.6f, 1.25f, 11.3f) - origin;
            Assert.IsTrue(Physics.Raycast(origin, direction, out var hit, 5f));
            StringAssert.StartsWith("Electrical", hit.collider.gameObject.name);
        }
        finally { Object.DestroyImmediate(camera.transform.parent.gameObject); }
    }
}
