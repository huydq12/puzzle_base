using System;
using UnityEngine;
using DG.Tweening;

public abstract class UIElement : MonoBehaviour
{
    private Action onHidden;
    public abstract bool ManualHide { get; }
    public abstract bool DestroyOnHide { get; }
    public abstract bool UseBehindPanel { get; }
    [SerializeField] public GameObject holder;

    protected void KillTweensInHierarchy(bool complete = false)
    {
        this.DOKill(complete);

        var transforms = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            var t = transforms[i];
            if (t == null) continue;
            t.DOKill(complete);
        }
    }

    public virtual void Show(Action hidden)
    {
        onHidden = hidden;
        Show();
    }
    public virtual void Show()
    {
        KillTweensInHierarchy(false);
        GameUI.Instance.Submit(this);
        if (holder != null)
            holder?.SetActive(true);
    }
    public virtual void Hide()
    {
        KillTweensInHierarchy(false);
        GameUI.Instance.Unsubmit(this);
        onHidden?.Invoke();
        if (DestroyOnHide)
        {
            GameUI.Instance.Unregister(this);
            Destroy(gameObject);
        }
        else
        {
            if (holder != null)
                holder?.SetActive(false);
        }
    }



    public virtual void OnShow() { }
    public virtual void OnHide() { }

    protected virtual void Awake()
    {
        GameUI.Instance.Register(this);
    }

    protected virtual void OnDestroy()
    {
        KillTweensInHierarchy(false);
    }
}
