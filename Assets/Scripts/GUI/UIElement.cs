using System;
using UnityEngine;

public abstract class UIElement : MonoBehaviour
{
    private Action onHidden;
    public abstract bool ManualHide { get; }
    public abstract bool DestroyOnHide { get; }
    public abstract bool UseBehindPanel { get; }
    [SerializeField] public GameObject holder;
    public virtual void Show(Action hidden)
    {
        onHidden = hidden;
        Show();
    }
    public virtual void Show()
    {
        GameUI.Instance.Submit(this);
        if (holder != null)
            holder?.SetActive(true);
    }
    public virtual void Hide()
    {
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
}