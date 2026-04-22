public static class SelectedBattleLevel
{
    public static string LevelFileName { get; private set; }

    public static void SetLevel(string levelFileName)
    {
        LevelFileName = levelFileName;
    }

    public static void Clear()
    {
        LevelFileName = null;
    }
}