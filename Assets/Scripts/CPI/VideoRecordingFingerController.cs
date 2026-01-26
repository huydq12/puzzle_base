using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

[RequireComponent(typeof(LazyTransform))]
public class VideoRecordingFingerController : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public LazyTransform lazyTransform;
    public float mouseDownScale = 0.9f;
    void Start()
    {
        GameManagerInGame.Instance.OnEndLevel += () =>
        {
           // canvasGroup.DOFade(0, 0.25F);
        };
        GameManagerInGame.Instance.OnStartLevel += () =>
        {
            transform.position = Input.mousePosition;
            lazyTransform.Position = Input.mousePosition;

            canvasGroup.DOFade(1, 0.25F);
        };
    }
    private void Update()
    {
    
        lazyTransform.Position = Input.mousePosition;
        if (Input.GetMouseButtonDown(0))
        {
            lazyTransform.LocalScale = Vector3.one * mouseDownScale;
        }
        if (Input.GetMouseButtonUp(0))
        {
            lazyTransform.LocalScale = Vector3.one;
        }
    }
}