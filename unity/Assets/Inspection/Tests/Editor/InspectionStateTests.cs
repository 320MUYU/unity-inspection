using NUnit.Framework;

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
}
