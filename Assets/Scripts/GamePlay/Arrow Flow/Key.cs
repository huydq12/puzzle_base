using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Key : MonoBehaviour
{
    [SerializeField] private Vector3 _offset = new Vector3(0f, 0.4f, 0f);

    [SerializeField] private GameObject _trail;

    public void SetAnchorPosition(Vector3 anchorPosition)
    {
        transform.position = anchorPosition + _offset;
    }
}
