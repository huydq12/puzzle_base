using System;
using DG.Tweening;
using UnityEngine;

public abstract class BaseUIElement : MonoBehaviour
{
    private Action onHidden;

    [SerializeField] public GameObject holder;

    protected UIType _uiType = UIType.Unknow;
    protected bool _isHide = true;
    protected bool _isInited;
    protected object _data;

    public virtual bool ManualHide => false;
    public virtual bool DestroyOnHide => false;
    public virtual bool UseBehindPanel => false;

    public bool IsInited => _isInited;
    public bool IsHide => _isHide;
    public UIType UIType => _uiType;
    public UIRegisterType UIRegisterType { get; set; }

    public virtual void Init()
    {
        if (_isInited) return;
        _isInited = true;
    }

    public void SetUIType(UIType type)
    {
        _uiType = type;
    }

    public void SetObjectData(object data)
    {
        _data = data;
    }

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
        BeforeShow();

        _isHide = false;
        GameUI.Instance.Submit(this);

        if (holder != null)
        {
            holder.SetActive(true);
        }
        else
        {
            gameObject.SetActive(true);
        }

        AfterShow();
    }

    public virtual void Hide()
    {
        KillTweensInHierarchy(false);
        BeforeHide();

        _isHide = true;
        GameUI.Instance.Unsubmit(this);

        onHidden?.Invoke();
        onHidden = null;

        if (DestroyOnHide)
        {
            GameUI.Instance.Unregister(this);
            Destroy(gameObject);
        }
        else
        {
            if (holder != null)
            {
                holder.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        AfterHide();
    }

    public void HideUI()
    {
        Hide();
    }

    public virtual void BeforeShow()
    {
    }

    public virtual void BeforeHide()
    {
    }

    public virtual void AfterShow()
    {
        OnShow();
    }

    public virtual void AfterHide()
    {
        OnHide();
    }

    public virtual void OnShow()
    {
    }

    public virtual void OnHide()
    {
    }

    protected virtual void Awake()
    {
        GameUI.Instance.Register(this);
    }

    protected virtual void OnDestroy()
    {
        KillTweensInHierarchy(false);
    }
}

public enum UIType
{
    Unknow,
    Screen,
    Popup,
    Notify,
    Overlap
}

public enum UIRegisterType
{
    None,
    OnScene,
    OnSpawn
}
