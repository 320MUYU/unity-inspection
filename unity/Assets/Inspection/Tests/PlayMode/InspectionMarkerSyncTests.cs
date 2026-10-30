using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class InspectionMarkerSyncTests
{
    [UnityTest]
    public IEnumerator ReconcileOnlyRemovesTheResolvedOrDeletedIssueAndRestoresReopened()
    {
        var host = new GameObject("Marker test");
        var registry = host.AddComponent<InspectionMarkerSync>();
        var first = new InspectionIssue { id = "one", status = "open", position = new InspectionPosition { x = 1, y = 2, z = 3 } };
        var second = new InspectionIssue { id = "two", status = "in_progress", position = new InspectionPosition { x = 2, y = 2, z = 3 } };
        try
        {
            registry.Reconcile(new[] { first, second });
            Assert.IsNotNull(registry.GetMarker("one"));
            var other = registry.GetMarker("two");
            Assert.IsNotNull(other);
            first.status = "resolved";
            registry.Reconcile(new[] { first, second });
            Assert.IsNull(registry.GetMarker("one"));
            Assert.AreSame(other, registry.GetMarker("two"));
            first.status = "open";
            registry.Reconcile(new[] { first, second });
            Assert.IsNotNull(registry.GetMarker("one"));
            registry.Reconcile(new[] { first });
            Assert.IsNull(registry.GetMarker("two"));
            Assert.IsNotNull(registry.GetMarker("one"));
            yield return null;
        }
        finally { Object.Destroy(host); }
    }

    [UnityTest]
    public IEnumerator RegisterKeepsTheSubmittedMarkerWithoutDuplicatingIt()
    {
        var host = new GameObject("Marker test");
        var registry = host.AddComponent<InspectionMarkerSync>();
        var marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        try
        {
            registry.Register("id", marker);
            registry.Reconcile(new[] { new InspectionIssue { id = "id", status = "open", position = new InspectionPosition() } });
            Assert.AreSame(marker, registry.GetMarker("id"));
            yield return null;
        }
        finally { Object.Destroy(host); }
    }
}
