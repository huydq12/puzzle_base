using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

public class Slot : MonoBehaviour
{
    [ReadOnly] public Cube CubeOnSlot;
    public bool IsOccupied => CubeOnSlot != null;
    public Tween AssignCube(Cube cube, bool inmediate)
    {
        CubeOnSlot = cube;
        cube.transform.SetParent(transform);
        cube.transform.localRotation = Quaternion.identity;
        cube.transform.localScale = 0.2f * Vector3.one;
        if (inmediate)
        {
            cube.transform.localPosition = Vector3.zero;
            return null;
        }
        else
        {
            float baseDuration = 0.125f;
            float speed = Board.Instance.Speed;
            float duration = baseDuration / speed;
            return cube.transform.DOLocalMove(Vector3.zero, duration);
        }
    }
}
