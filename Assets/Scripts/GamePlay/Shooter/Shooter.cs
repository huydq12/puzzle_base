using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEditor;


public class Shooter : MonoBehaviour
{
    [ReadOnly] public Vector2Int GridPosition;
    [SerializeField] private Renderer _renderer;
    [SerializeField] private TextMeshPro _text;
    [SerializeField] private Animation _animation;
    [SerializeField] private Outline _outline;
    [ReadOnly] public ObjectColor Color;
    [ReadOnly] public bool IsMoving;
    public bool OnHolder
    {
        get => _text.enabled;
        set => _text.enabled = value;
    }
    public GameColorConfig ColorConfig;
    public void Setup(ObjectColor color, int total)
    {
        _renderer.materials = new Material[]
        {
            ColorConfig.GetShooterColor(color),
            ColorConfig.GetShooterEye(color)
        };
        _text.text = total.ToString();
        OnHolder = false;
    }
    public void OnValidate()
    {
        if (_renderer == null)
        {
            _renderer = GetComponentInChildren<Renderer>();
        }
        if (_text == null)
        {
            _text = GetComponentInChildren<TextMeshPro>();
            if (_text == null)
            {
                string path = "Assets/_GameAssets/Prefabs/Text (TMP).prefab";

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    Debug.LogError($"Không load được prefab tại path: {path}");
                    return;
                }

                GameObject go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                go.transform.SetParent(transform, false);
                _text = go.GetComponent<TextMeshPro>();
                PrefabUtility.UnpackPrefabInstance(
                go,
                PrefabUnpackMode.Completely,
                InteractionMode.AutomatedAction
            );
            }
        }
        if (_animation == null)
        {
            _animation = GetComponentInChildren<Animation>();
        }
    }


}
