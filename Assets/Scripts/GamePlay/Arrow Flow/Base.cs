using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

public class Base : MonoBehaviour
{
    public CubeLine CubeOnBase;
    public bool IsOccupied => CubeOnBase != null;

    public void AssignCube(CubeLine cube, float moveTime)
    {
        CubeOnBase = cube;
        cube.transform.DOKill();
        if (moveTime <= 0f)
        {
            cube.transform.position = transform.position;
        }
        else
        {
            cube.transform.DOMove(transform.position, moveTime).SetEase(Ease.Linear);
        }
    }
}
