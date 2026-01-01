using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CameraFollowSmoth : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset;
    [SerializeField] private float daming;
    public GameObject text;
    private Vector3 velocity = Vector3.zero;

    private void FixedUpdate()
    {

        if (target.position.x >= -2.2f)
        {
            Vector3 movePosition = target.position + offset;

            transform.position = Vector3.SmoothDamp(transform.position, movePosition, ref velocity, daming);
            transform.position = new Vector3(transform.position.x, 0, -15.7f);
        }

    }

    //private void Update()
    //{
    //    text.transform.position = Camera.main.WorldToScreenPoint(target.transform.position);
    //}

}
