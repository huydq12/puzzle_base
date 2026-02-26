using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Bomb : MonoBehaviour
{
    [SerializeField] private TextMeshPro _counterTimeText;
    [SerializeField] private Vector3 _counterTextOffset = new Vector3(0f, 0.4f, 0f);

    [SerializeField] private Animator _animator;
    
}
