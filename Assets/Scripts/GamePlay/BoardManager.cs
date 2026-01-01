using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager : Singleton<BoardManager>
{
    [SerializeField] private Transform boardRoot;
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private GameObject shooterPrefab;
    [SerializeField] private GameObject shooterUnitPrefab;
    [SerializeField] private GameObject conveyorPrefab;

    [SerializeField] private string levelResourcesFolder = "Levels";
    [SerializeField] private string colorConfigResourceName = "Color game Config";

    private readonly List<GameObject> spawned = new List<GameObject>();
    private GameColorConfig colorConfig;

    [SerializeField] private int level = 0;

    private void Start()
    {
        LoadLevel(level);
    }

    public void LoadLevel(int level)
    {
        EnsureConfig();
        ClearBoard();

        TextAsset textAsset = Resources.Load<TextAsset>($"{levelResourcesFolder}/Level_{level}");
        if (textAsset == null)
        {
            Debug.LogError($"Missing level json: Resources/{levelResourcesFolder}/Level_{level}.json");
            return;
        }

        LevelData data = JsonUtility.FromJson<LevelData>(textAsset.text);
        if (data == null)
        {
            Debug.LogError($"Invalid level json: Level_{level}");
            return;
        }

        SpawnArrows(data);
        SpawnShooters(data);
        SpawnConveyors(data);
    }

    public void ClearBoard()
    {
        for (int i = spawned.Count - 1; i >= 0; i--)
        {
            if (spawned[i] != null)
                Destroy(spawned[i]);
        }
        spawned.Clear();
    }

    private void EnsureConfig()
    {
        if (colorConfig != null) return;
        colorConfig = Resources.Load<GameColorConfig>(colorConfigResourceName);
    }

    private Transform RootOrSelf()
    {
        return boardRoot != null ? boardRoot : transform;
    }

    private void SpawnArrows(LevelData data)
    {
        if (arrowPrefab == null) return;
        if (data.arrows == null) return;

        Transform root = RootOrSelf();

        for (int i = 0; i < data.arrows.Count; i++)
        {
            ArrowData arrow = data.arrows[i];
            Vector3 pos = Vector3.zero;
            if (arrow.unitPositions != null && arrow.unitPositions.Count > 0)
            {
                UnitPosition u = arrow.unitPositions[0];
                pos = new Vector3(u.x, 0f, u.y);
            }
            GameObject go = Instantiate(arrowPrefab, pos, Quaternion.identity, root);
            spawned.Add(go);

            ApplyMaterial(go, (ObjectColor)arrow.color, true);
        }
    }

    private void SpawnShooters(LevelData data)
    {
        if (shooterPrefab == null) return;
        if (data.shooters == null) return;

        Transform root = RootOrSelf();

        for (int i = 0; i < data.shooters.Count; i++)
        {
            ShooterData shooter = data.shooters[i];
            Vector3 pos = new Vector3(shooter.position.x, 0f, shooter.position.y);
            Quaternion rot = DirectionToRotation(shooter.direction);

            GameObject shooterGo = Instantiate(shooterPrefab, pos, rot, root);
            spawned.Add(shooterGo);

            if (shooter.shooterUnits == null || shooter.shooterUnits.Count == 0) continue;
            if (shooterUnitPrefab == null) continue;

            Transform currentHolder = FindChild(shooterGo.transform, "CurrentShooterHolder");
            Transform nextHolder = FindChild(shooterGo.transform, "NextShooterHolder");
            Transform nextNextHolder = FindChild(shooterGo.transform, "NextNextShooterHolder");

            for (int u = 0; u < shooter.shooterUnits.Count; u++)
            {
                ShooterUnitData unit = shooter.shooterUnits[u];
                Transform holder = u == 0 ? currentHolder : (u == 1 ? nextHolder : nextNextHolder);
                if (holder == null) holder = shooterGo.transform;

                GameObject unitGo = Instantiate(shooterUnitPrefab, holder.position, holder.rotation, holder);
                spawned.Add(unitGo);
                ApplyMaterial(unitGo, (ObjectColor)unit.color, false);
            }
        }
    }

    private void SpawnConveyors(LevelData data)
    {
        if (conveyorPrefab == null) return;
        if (data.conveyors == null) return;

        Transform root = RootOrSelf();

        for (int i = 0; i < data.conveyors.Count; i++)
        {
            ConveyorData conveyor = data.conveyors[i];
            if (conveyor.conveyorNodes == null || conveyor.conveyorNodes.Count == 0) continue;
            Vector3 pos = conveyor.conveyorNodes[0].position.ToVector3();
            GameObject go = Instantiate(conveyorPrefab, pos, Quaternion.identity, root);
            spawned.Add(go);
        }
    }

    private void ApplyMaterial(GameObject go, ObjectColor color, bool isCube)
    {
        if (colorConfig == null) return;

        Material mat = isCube ? colorConfig.GetCubeByColor(color) : colorConfig.GetShooterByColor(color);
        if (mat == null) return;

        var meshRenderers = go.GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < meshRenderers.Length; i++)
        {
            if (meshRenderers[i] == null) continue;
            var mats = meshRenderers[i].sharedMaterials;
            if (mats == null || mats.Length == 0)
            {
                meshRenderers[i].sharedMaterial = mat;
                continue;
            }
            mats[0] = mat;
            meshRenderers[i].sharedMaterials = mats;
        }

        var skinned = go.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < skinned.Length; i++)
        {
            if (skinned[i] == null) continue;
            var mats = skinned[i].sharedMaterials;
            if (mats == null || mats.Length == 0)
            {
                skinned[i].sharedMaterial = mat;
                continue;
            }
            mats[0] = mat;
            skinned[i].sharedMaterials = mats;
        }
    }

    private static Transform FindChild(Transform root, string name)
    {
        if (root == null) return null;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == name) return child;
            Transform found = FindChild(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private static Quaternion DirectionToRotation(int direction)
    {
        float y = 0f;
        if (direction == 1) y = 90f;
        else if (direction == 2) y = 180f;
        else if (direction == 3) y = 270f;
        else if (direction == 4) y = 0f;
        return Quaternion.Euler(0f, y, 0f);
    }

    [System.Serializable]
    private class LevelData
    {
        public List<ArrowData> arrows;
        public List<ShooterData> shooters;
        public List<ConveyorData> conveyors;
    }

    [System.Serializable]
    private class ArrowData
    {
        public List<UnitPosition> unitPositions;
        public int color;
        public int elementType;
        public int counter;
        public int arrowID;
    }

    [System.Serializable]
    private class UnitPosition
    {
        public int x;
        public int y;
    }

    [System.Serializable]
    private class ShooterData
    {
        public Float2 position;
        public int direction;
        public List<ShooterUnitData> shooterUnits;
    }

    [System.Serializable]
    private class ShooterUnitData
    {
        public int color;
        public int counter;
        public int type;
    }

    [System.Serializable]
    private class ConveyorData
    {
        public List<ConveyorNodeData> conveyorNodes;
    }

    [System.Serializable]
    private class ConveyorNodeData
    {
        public Vector3Data position;
        public bool isHole;
    }

    [System.Serializable]
    private class Float2
    {
        public float x;
        public float y;
    }

    [System.Serializable]
    private class Vector3Data
    {
        public float x;
        public float y;
        public float z;

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }
    }
}
