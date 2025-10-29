public static class CardDispenseRuntime
{
    private static int _activeDispenseCount = 0;

    public static bool IsLocked => _activeDispenseCount > 0;

    public static void NotifyStarted()
    {
        _activeDispenseCount++;
        if (_activeDispenseCount < 0) _activeDispenseCount = 0;
    }

    public static void NotifyFinished()
    {
        _activeDispenseCount--;
        if (_activeDispenseCount < 0) _activeDispenseCount = 0;
    }
}


