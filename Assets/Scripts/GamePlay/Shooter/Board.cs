using Sirenix.OdinInspector;
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
    [SerializeField] private float _speed = 1f;
    [SerializeField] private Cube _cubePrefab;
    [SerializeField] private Base _basePrefab;
    [SerializeField] private GameObject _arrowPrefab;
    [SerializeField] private GameColorConfig _colorConfig;

    [ReadOnly] public int CurrentLevelIndex;
    [ReadOnly] public LevelMap CurrentMap;

    public GameColorConfig ColorConfig => _colorConfig;
    public float Speed => _speed;
    public Base BasePrefab => _basePrefab;
    public Cube CubePrefab => _cubePrefab;

    protected override void Awake()
    {
        base.Awake();
    }

    public void SetupLevel(int level, LevelMap map)
    {
        if (map == null)
        {
            Debug.LogError("Board.SetupLevel: LevelMap is null!");
            return;
        }

        if (GameManagerInGame.Instance == null)
        {
            Debug.LogError("Board.SetupLevel: GameManagerInGame.Instance is null!");
            return;
        }

        GameManagerInGame.Instance.SetState(GameStateInGame.Init);

        CurrentLevelIndex = level;
        CurrentMap = map;

        InitializeMap(map);

        GameManagerInGame.Instance.SetState(GameStateInGame.Playing);
    }

    private void InitializeMap(LevelMap map)
    {
        map.transform.eulerAngles = new Vector3(0, 180, 0);
        map.GenerateBasesOnConveyor();
        map.GenerateBasesOnConveyorQueue();
        map.SpawnArrowAlongSpline(_arrowPrefab);
    }
}