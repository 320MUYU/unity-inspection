using NUnit.Framework;
using UnityEngine;

public class InspectionStateTests
{
    [Test]
    public void ModalPreventsAdditionalPlacementUntilCancelled()
    {
        var state = new InspectionState();
        Assert.IsTrue(state.TryOpen());
        Assert.IsFalse(state.TryOpen());
        state.Close();
        Assert.IsTrue(state.TryOpen());
    }

    [TestCase(null, false)]
    [TestCase("  ", false)]
    [TestCase("机柜异常", true)]
    public void TitleMustNotBeBlank(string title, bool expected)
    {
        Assert.AreEqual(expected, InspectionState.ValidTitle(title));
    }

    [Test]
    public void RoomContainsInspectableEquipmentAndRayHitsCabinet()
    {
        var camera = InspectionScene.Build();
        try
        {
            Assert.IsNotNull(GameObject.Find("Cabinet A-01"));
            Assert.IsNotNull(GameObject.Find("Cabinet A-02"));
            Assert.IsNotNull(GameObject.Find("Electrical cabinet"));
            Physics.SyncTransforms();
            var target = new Vector3(-2.8f, 1.1f, 5.6f);
            Assert.IsTrue(Physics.Raycast(camera.transform.position, target - camera.transform.position, out var hit, 20f));
            StringAssert.Contains("A-01", hit.collider.gameObject.name);
        }
        finally { Object.DestroyImmediate(camera.transform.parent.gameObject); }
    }
}
