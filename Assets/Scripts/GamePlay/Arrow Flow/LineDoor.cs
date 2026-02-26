using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class LineDoor : MonoBehaviour
{
    [Serializable]
    public class LineDoorColor
    {
        public ObjectColor Color;
        public UnityEngine.Color Value = UnityEngine.Color.white;
    }

    [SerializeField] private Animator animator;

    [SerializeField] private CubeEffect cubeEffect;

    [SerializeField] private ParticleSystem vfx;

    [SerializeField] private Transform pointArrow;

    [SerializeField] private TextMeshPro countText;

    [SerializeField] private SpriteRenderer spriteRendererColor;
    [SerializeField] private List<LineDoorColor> colorTable = new();

    [Header("Animations")]
    [SerializeField] private string _decreaseAnim = "Decrease";
    [SerializeField] private string _openAnim = "Open";
    [SerializeField] private AnimationClip _decreaseClip;
    [SerializeField] private AnimationClip _openClip;

    public ObjectColor Color { get; private set; }
    public int Remaining { get; private set; }
    public bool IsOpened { get; private set; }

    public Transform ArrowPoint => pointArrow != null ? pointArrow : transform;

    public void Setup(ObjectColor color, int counter)
    {
        Color = color;
        Remaining = Mathf.Max(0, counter);
        IsOpened = false;
        ApplyColor();
        UpdateCounterText();
    }

    public bool Consume(int amount, Action onOpened = null)
    {
        if (IsOpened) return false;
        if (amount <= 0) return false;

        int next = Mathf.Max(0, Remaining - amount);
        if (next == Remaining) return false;

        Remaining = next;
        UpdateCounterText();
        PlayDecrease();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[LineDoor] Consume color={Color} remaining={Remaining} amount={amount}", this);
#endif

        if (Remaining <= 0)
        {
            Open(onOpened);
            return true;
        }

        return false;
    }

    public void Open(Action onOpened = null)
    {
        if (IsOpened) return;
        IsOpened = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[LineDoor] Open color={Color}", this);
#endif

        if (animator != null && !string.IsNullOrEmpty(_openAnim))
            animator.Play(_openAnim, 0, 0f);

        if (vfx != null)
        {
            if (pointArrow != null)
                vfx.transform.position = pointArrow.position;
            vfx.Play(true);
        }

        float delay = GetAnimationLength(_openAnim, _openClip);
        if (delay <= 0f)
        {
            onOpened?.Invoke();
            Destroy(gameObject);
        }
        else
        {
            StartCoroutine(OpenRoutine(delay, onOpened));
        }
    }

    private IEnumerator OpenRoutine(float delay, Action onOpened)
    {
        yield return new WaitForSeconds(delay);
        onOpened?.Invoke();
        Destroy(gameObject);
    }

    private void UpdateCounterText()
    {
        if (countText == null) return;
        countText.text = Remaining.ToString();
    }

    private void ApplyColor()
    {
        if (spriteRendererColor == null) return;
        UnityEngine.Color c = UnityEngine.Color.white;
        if (colorTable != null)
        {
            for (int i = 0; i < colorTable.Count; i++)
            {
                LineDoorColor entry = colorTable[i];
                if (entry == null) continue;
                if (entry.Color != Color) continue;
                c = entry.Value;
                break;
            }
        }
        spriteRendererColor.color = c;
    }

    private void PlayDecrease()
    {
        if (animator == null || string.IsNullOrEmpty(_decreaseAnim)) return;
        animator.Play(_decreaseAnim, 0, 0f);
    }

    private float GetAnimationLength(string animName, AnimationClip clipOverride = null)
    {
        if (clipOverride != null) return clipOverride.length;
        if (animator == null || animator.runtimeAnimatorController == null) return 0f;
        AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
        foreach (AnimationClip clip in clips)
        {
            if (clip != null && clip.name == animName)
                return clip.length;
        }
        return 0f;
    }
}
