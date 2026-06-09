public static class GamePauseState
{
    public static bool IsLevelUpSelectionOpen { get; private set; }
    public static bool IsForcedPause { get; private set; }
    public static bool IsGameplayPaused => IsLevelUpSelectionOpen || IsForcedPause;

    [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetState()
    {
        IsLevelUpSelectionOpen = false;
        IsForcedPause = false;
    }

    public static void SetLevelUpSelectionOpen(bool isOpen)
    {
        IsLevelUpSelectionOpen = isOpen;
    }

    public static void SetForcedPause(bool isPaused)
    {
        IsForcedPause = isPaused;
    }
}
