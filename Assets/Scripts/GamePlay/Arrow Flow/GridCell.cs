using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
public enum GridCellType
{
    Normal,
    Conveyor
}
public class GridCell : MonoBehaviour
{
    [ReadOnly] public GridCellType CellType;
    [ReadOnly] public Vector2Int Position;
    [ReadOnly] public CubeLine CubeOnCell;
    [SerializeField] private Renderer _renderer;
    public bool IsOccupied => CubeOnCell != null;
    public void ShowRenderer(bool show)
    {
        if (_renderer != null)
        {
            _renderer.enabled = show;
        }
    }

}
