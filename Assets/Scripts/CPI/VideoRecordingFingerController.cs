using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Spine.Unity;
using UnityEngine;

[RequireComponent(typeof(LazyTransform))]
public class VideoRecordingFingerController : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public LazyTransform lazyTransform;
    public SkeletonGraphic Skeleton;

    [Header("Spine Animation Names")]
    public string tapAnimationName = "Tap";   // Tên animation khi click

    void Start()
    {
        GameManagerInGame.Instance.OnStartLevel += () =>
        {
            transform.position = Input.mousePosition;
            lazyTransform.Position = Input.mousePosition;
            canvasGroup.DOFade(1, 0.25f);
        };
    }

    private void Update()
    {
        lazyTransform.Position = Input.mousePosition;

        if (Input.GetMouseButtonDown(0))
        {
            if (Skeleton != null)
            {
                Skeleton.AnimationState.SetAnimation(0, tapAnimationName, false);
            }
        }
    }
}