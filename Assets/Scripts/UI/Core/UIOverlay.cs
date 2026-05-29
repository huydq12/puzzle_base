using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class UIOverlay : MonoBehaviour
{
    private CanvasGroup _canvasGroup;
    private Image _image;
    private Tween _tween;
    private Transform _parent;

    public void Init(Transform parent)
    {
        this._parent = parent;
        // CanvasGroup
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        // Image
        _image = GetComponent<Image>();
        if (_image == null)
            _image = gameObject.AddComponent<Image>();

        _image.color = Color.black;

        _canvasGroup.alpha = 0;
        _canvasGroup.blocksRaycasts = false;

        gameObject.SetActive(false);
    }

    public void ApplyConfig(UIOverlayConfig config)
    {
        int lastIndex = _parent.childCount - 1;
        int overlayIndex = lastIndex - 1;
        transform.SetSiblingIndex(overlayIndex);

        if (config == null) return;

        _image.sprite = config.sprite;
        _image.color = new Color(config.color.r, config.color.g, config.color.b, 1f);

        _canvasGroup.blocksRaycasts = config.raycast;
        _canvasGroup.alpha = config.alpha;
    }  
    
    public void ApplyConfig(UIOverlayConfig config, Transform target)
    {
        _parent = target.parent;
        transform.SetParent(_parent, false);
        ApplyConfig(config);
    }

    public void Show(UIOverlayConfig config, Transform target, float duration = 0.25f)
    {
        _tween?.Kill();

        Transform newParent = target.parent;
        _parent = newParent;

        transform.SetParent(newParent, false);

        transform.SetSiblingIndex(target.GetSiblingIndex() - 1);

        if (transform is RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        ApplyConfig(config);

        gameObject.SetActive(true);

        _tween = _canvasGroup.DOFade(config.alpha, duration);
    }

    public void Hide(float duration = 0.25f)
    {
        _tween?.Kill();

        _canvasGroup.blocksRaycasts = false;

        _tween = _canvasGroup.DOFade(0f, duration)
            .OnComplete(() =>
            {
                gameObject.SetActive(false);
            });
    }

    public bool IsVisible => _canvasGroup.alpha > 0.01f;
}

[System.Serializable]
public class UIOverlayConfig
{
    public Sprite sprite;
    public Color color = Color.black;
    [Range(0, 1)] public float alpha = 0.5f;
    public bool raycast = true;

    public static UIOverlayConfig Default =>
        new UIOverlayConfig
        {
            color = Color.black,
            alpha = 0.5f,
            raycast = true,
            sprite = null
        };
}