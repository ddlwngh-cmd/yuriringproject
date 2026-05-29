public static class GamePauseState
{
    public static bool IsLevelUpSelectionOpen { get; private set; }
    public static bool IsGameplayPaused => IsLevelUpSelectionOpen;

    public static void SetLevelUpSelectionOpen(bool isOpen)
    {
        IsLevelUpSelectionOpen = isOpen;
    }
}
