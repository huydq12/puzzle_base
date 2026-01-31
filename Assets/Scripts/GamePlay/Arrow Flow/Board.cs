
using UnityEngine;

public enum BoosterType
{
    None,
    Hammer,
    Conveyor,
    Rainbow
}
public class Board : Singleton<Board>
{
    [SerializeField] private GameColorConfig _colorConfig;
    public GameColorConfig ColorConfig => _colorConfig;
    private LevelConfig _currentConfig;



    protected override void Awake()
    {
        base.Awake();
    }

    

    private static Camera FindMainCameraFallback()
    {
        Camera[] cams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        for (int i = 0; i < cams.Length; i++)
        {
            Camera cam = cams[i];
            if (cam != null && cam.CompareTag("MainCamera"))
            {
                return cam;
            }
        }

        return cams.Length > 0 ? cams[0] : null;
    }

    private static Camera FindEffectCameraFallback(Camera main)
    {
        Camera[] cams = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        for (int i = 0; i < cams.Length; i++)
        {
            Camera cam = cams[i];
            if (cam == null || cam == main) continue;
            if (!cam.CompareTag("MainCamera")) return cam;
        }

        return null;
    }

   
    public void SetupLevel(LevelConfig config)
    {
        GameManagerInGame.Instance.SetState(GameStateInGame.Init);
        _currentConfig = config;
      
        GameManagerInGame.Instance.SetState(GameStateInGame.Playing);
    }
}
