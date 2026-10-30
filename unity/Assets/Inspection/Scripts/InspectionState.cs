public sealed class InspectionState
{
    public bool IsOpen { get; private set; }
    public bool TryOpen() { if (IsOpen) return false; IsOpen = true; return true; }
    public void Close() { IsOpen = false; }
    public static bool ValidTitle(string title) { return !string.IsNullOrWhiteSpace(title); }
}
