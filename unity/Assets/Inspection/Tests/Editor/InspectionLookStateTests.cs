using NUnit.Framework;

public class InspectionLookStateTests
{
    [Test]
    public void CaptureConsumesTheClickThatResumesInspection()
    {
        var state = new InspectionLookState();
        Assert.IsFalse(state.IsCaptured);
        Assert.IsTrue(state.TryCapture(10));
        Assert.IsTrue(state.IsCaptured);
        Assert.IsFalse(state.CanMark(10));
        Assert.IsTrue(state.CanMark(11));
        state.Release();
        Assert.IsFalse(state.CanMark(12));
        Assert.IsTrue(state.TryCapture(12));
        Assert.IsFalse(state.CanMark(12));
        Assert.IsTrue(state.CanMark(13));
    }

    [Test]
    public void MouseMovementTurnsOnlyWhileCapturedAndClampsPitch()
    {
        var state = new InspectionLookState();
        state.TryCapture(0);
        state.ApplyMouseDelta(4, 3, 2);
        Assert.AreEqual(8f, state.Yaw);
        Assert.AreEqual(-6f, state.Pitch);
        state.ApplyMouseDelta(0, 100, 2);
        Assert.AreEqual(-80f, state.Pitch);
        state.ApplyMouseDelta(0, -100, 2);
        Assert.AreEqual(80f, state.Pitch);
        state.Release();
        state.ApplyMouseDelta(100, 100, 2);
        Assert.AreEqual(8f, state.Yaw);
        Assert.AreEqual(80f, state.Pitch);
    }

    [Test]
    public void ModalReleasesCaptureAndClosingSuppressesTheSameFrame()
    {
        var state = new InspectionLookState();
        state.TryCapture(0);
        Assert.IsTrue(state.OpenModal());
        Assert.IsTrue(state.IsModalOpen);
        Assert.IsFalse(state.IsCaptured);
        Assert.IsFalse(state.TryCapture(1));
        Assert.IsFalse(state.CanMark(1));
        state.ApplyMouseDelta(5, 5, 1);
        Assert.AreEqual(0f, state.Yaw);
        Assert.IsTrue(state.CloseModal(2));
        Assert.IsTrue(state.IsCaptured);
        Assert.IsFalse(state.CanMark(2));
        Assert.IsTrue(state.CanMark(3));
    }

    [Test]
    public void FocusLossReleasesCursorAndRequiresExplicitRecapture()
    {
        var state = new InspectionLookState();
        state.TryCapture(0);
        state.SetFocus(false);
        Assert.IsFalse(state.IsFocused);
        Assert.IsFalse(state.IsCaptured);
        Assert.IsFalse(state.TryCapture(1));
        state.SetFocus(true);
        Assert.IsFalse(state.IsCaptured);
        Assert.IsTrue(state.TryCapture(2));
        Assert.IsFalse(state.CanMark(2));
    }

    [Test]
    public void ClosingModalWhileUnfocusedDoesNotCaptureCursor()
    {
        var state = new InspectionLookState();
        state.TryCapture(0);
        state.OpenModal();
        state.SetFocus(false);
        Assert.IsTrue(state.CloseModal(2));
        Assert.IsFalse(state.IsCaptured);
        state.SetFocus(true);
        Assert.IsFalse(state.IsCaptured);
        Assert.IsTrue(state.TryCapture(3));
        Assert.IsFalse(state.CanMark(3));
    }
}
