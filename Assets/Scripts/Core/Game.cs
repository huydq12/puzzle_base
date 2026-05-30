using UnityEngine;

public class Game : MonoBehaviour
{
    public static GameData Data { get; private set; }
    public static GameAsset Asset { get; private set; }
    public static GameConfig Config { get; private set; }
    public static GameUpdate Update { get; private set; }
    public static bool IsLaunched { get; private set; }
    private void OnApplicationQuit()
    {
        Data.SaveAll();
    }
    private void OnApplicationPause(bool pause)
    {
        if (pause) Data.SaveAll();
    }
    public static void Launch()
    {
        if (IsLaunched) return;
        Application.targetFrameRate = 60;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        GameObject gameLauncher = new GameObject("GameLauncher");
        gameLauncher.AddComponent<Game>();
        Update = gameLauncher.AddComponent<GameUpdate>();
        Data = new GameData();
        Config = new GameConfig();
        Asset = new GameAsset();
        DontDestroyOnLoad(gameLauncher);
        IsLaunched = true;
    }
}