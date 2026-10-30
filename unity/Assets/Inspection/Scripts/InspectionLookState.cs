using UnityEngine;

/// <summary>
/// Tracks inspection focus and look state without directly manipulating the OS cursor.
/// The host applies cursor visibility and lock state from <see cref="IsCaptured"/>.
/// </summary>
public sealed class InspectionLookState
{
    private int blockedThroughFrame = -1;

    public bool IsFocused { get; private set; } = true;
    public bool IsCaptured { get; private set; }
    public bool IsModalOpen { get; private set; }
    public bool CanLook => IsFocused && IsCaptured && !IsModalOpen;
    public float Yaw { get; private set; }
    public float Pitch { get; private set; }

    public bool TryCapture(int frame)
    {
        if (!IsFocused || IsModalOpen || IsCaptured) return false;
        IsCaptured = true;
        blockedThroughFrame = frame;
        return true;
    }

    public void Release()
    {
        IsCaptured = false;
    }

    public void SetFocus(bool focused)
    {
        IsFocused = focused;
        if (!focused) Release();
    }

    public bool OpenModal()
    {
        if (!CanLook) return false;
        IsModalOpen = true;
        Release();
        return true;
    }

    public bool CloseModal(int frame)
    {
        if (!IsModalOpen) return false;
        IsModalOpen = false;
        if (IsFocused) TryCapture(frame);
        return true;
    }

    public bool CanMark(int frame)
    {
        return CanLook && frame > blockedThroughFrame;
    }

    public void ApplyMouseDelta(float horizontal, float vertical, float sensitivity)
    {
        if (!CanLook) return;
        Yaw += horizontal * sensitivity;
        Pitch = Mathf.Clamp(Pitch - vertical * sensitivity, -80f, 80f);
    }
}
