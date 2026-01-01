using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IceObject : MonoBehaviour
{
    [SerializeField] private List<GameObject> _iceObjects;
    public void SetHP(int value)
    {
        for (int i = 0; i < _iceObjects.Count; i++)
        {
            _iceObjects[i].SetActive(i < value);
        }
    }
}
