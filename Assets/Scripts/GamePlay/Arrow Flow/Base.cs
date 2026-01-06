using Dreamteck.Splines;
using Sirenix.OdinInspector;
using UnityEngine;

public class Base : MonoBehaviour
{
    public CubeLine CubeOnBase;
    public bool IsOccupied => CubeOnBase != null;
    public SplinePositioner Positioner => _spline;
    [SerializeField] private SplinePositioner _spline;

    public void AssignCube(CubeLine cube)
    {
        CubeOnBase = cube;
        cube.transform.SetParent(this.transform);
        cube.transform.localPosition = Vector3.zero;
    }
    public void RemoveCube()
    {
        CubeOnBase.transform.SetParent(null);
        CubeOnBase = null;
    }
}
