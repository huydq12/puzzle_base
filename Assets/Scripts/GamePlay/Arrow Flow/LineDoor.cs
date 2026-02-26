using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class LineDoor : MonoBehaviour
{
    [SerializeField] private Animator animator;

    [SerializeField] private ParticleSystem vfx;

    [SerializeField] private Transform pointArrow;

    [SerializeField] private TextMeshPro countText;
}
