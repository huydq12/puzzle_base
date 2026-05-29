using Cysharp.Threading.Tasks;
using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using UnityEngine;

public abstract class BaseUIElement : SerializedMonoBehaviour
{
    [SerializeField] public GameObject holder;

    public virtual bool ManualHide => false;
    public virtual bool DestroyOnHide => false;
    public virtual bool UseBehindPanel => false;

    public bool hasOverlay;
    [ShowIf(nameof(hasOverlay))]
    public UIOverlayConfig uIOverlayConfig;

    protected UIType _uiType = UIType.Unknow;
    protected bool _isHide;
    protected bool _isInited;

	protected Sequence _animationSequence;
	public UIAnimType animType;
	public UIAnimType currentAnimType;
	private Action _onHidden;

    public bool IsInited => _isInited;
    public bool IsHide => _isHide;
    public UIType UIType => _uiType;
    public UIRegisterType UIRegisterType { get; set; }

    protected object _data;

    public virtual void Init()
    {
        if (_isInited) return;
        _isInited = true;

        Register();
    }

    public void SetUIType(UIType type)
    {
        _uiType = type;
    }

    public void SetAnim(UIAnimType animType)
    {
        this.currentAnimType = animType;
    }

	protected virtual void Awake()
	{
		if (holder == null)
		{
			holder = gameObject;
		}
	}

	protected virtual void Start()
	{
	}

	protected virtual void OnDestroy()
	{
		KillTweensInHierarchy(false);

		if (_animationSequence != null)
		{
			_animationSequence.Kill();
			_animationSequence = null;
		}
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
		_onHidden = hidden;
		Show();
	}

	public virtual void Show()
	{
		ShowAsync().Forget();
	}

	public virtual void Hide()
	{
		_onHidden?.Invoke();
		_onHidden = null;
		HideAsync().Forget();
	}

    public virtual async UniTask ShowAsync()
    {
        BeforeShow();

        gameObject.SetActive(true);
        if (holder != null)
        {
            holder.SetActive(true);
        }
        _isHide = false;

        if (_animationSequence != null)
        {
            _animationSequence.Kill();
        }

        _animationSequence = UIAnimationFactory.CreateShow(this, currentAnimType);

        if (_animationSequence == null)
        {
            AfterShow();
            return;
        }

        await _animationSequence.AsyncWaitForCompletion();

        AfterShow();
        _animationSequence = null;
    }

    public virtual async UniTask HideAsync()
    {
        BeforeHide();

        if (_animationSequence != null)
        {
            _animationSequence.Kill();
        }

        _isHide = true;

        _animationSequence = UIAnimationFactory.CreateHide(this, currentAnimType);

        if (_animationSequence == null)
        {
            AfterHide();
            return;
        }

        await _animationSequence.AsyncWaitForCompletion();
        AfterHide();
        _animationSequence = null;
    }

    public void HideUI()
    {
        UIManager.Instance.HideUI(this.GetType());
    }

    public void SetObjectData(object data)
    {
        this._data = data;
    }

    public virtual void BeforeShow()
    {

    }

    public virtual void BeforeHide()
    {

    }

    public virtual void AfterShow() 
    {
    
    }

    public virtual void AfterHide() 
    {
        if (DestroyOnHide)
        {
            Destroy(gameObject);
            return;
        }

        if (holder != null)
        {
            holder.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    public virtual UIOverlayConfig GetOverlayConfig()
    {
        return uIOverlayConfig;
    }

    public void Register()
    {
        if (UIRegisterType != UIRegisterType.None) return;
        UIManager.Instance.RegisterUI(this);
        UIRegisterType = UIRegisterType.OnScene;
    }
}

public enum UIAnimType
{
    Default,
    None,
    Fade,
    SlideLeft,
    SlideRight,
    ScaleUp,
    Popup
}

public enum UIRegisterType
{
    None,
    OnScene,
    OnSpawn,
}
