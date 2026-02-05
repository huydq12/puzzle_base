
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
    [ReadOnly] public int CurrentLevelIndex;
    [ReadOnly] public LevelMap CurrentMap;
    [SerializeField] private float _speed;
    [SerializeField] private Cube _cubePrefab;
    [SerializeField] private Base _basePrefab;
    [SerializeField] private GameColorConfig _colorConfig;
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
        GameManagerInGame.Instance.SetState(GameStateInGame.Init);
        CurrentLevelIndex = level;
        CurrentMap = map;
        map.transform.eulerAngles = new Vector3(0, 180, 0);
        map.GenerateBasesOnConveyor();
        map.GenerateBasesOnConveyorQueue();
        GameManagerInGame.Instance.SetState(GameStateInGame.Playing);
    }
}
