using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandCursor : MonoBehaviour
{
    [SerializeField] Animation m_HandAnimation;
    [SerializeField] RectTransform m_CanvasRect;

#if UNITY_EDITOR
    RectTransform m_Rect;
#endif

    private void Awake()
    {
#if UNITY_EDITOR
        m_Rect = GetComponent<RectTransform>();
#else
        gameObject.SetActive(false);
#endif
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            m_HandAnimation.Play("Press");
        }
        else if (Input.GetMouseButtonUp(0))
        {
            m_HandAnimation.Play("Release");
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(m_CanvasRect, Input.mousePosition, null, out Vector2 vector);
        m_Rect.anchoredPosition = vector;
    }
#endif
}
