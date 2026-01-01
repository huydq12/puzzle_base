using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Base abstract class cho recyclable scroll với generic data type
/// </summary>
[RequireComponent(typeof(ScrollRect))]
public abstract class RecyclableScrollBase<TData> : MonoBehaviour
{
    [Header("Layout Settings")]
    [SerializeField] protected float _spacing;
    [SerializeField] protected RectOffset _padding;

    [Header("Item Settings")]
    [SerializeField] protected GameObject _itemPrefab;
    [SerializeField] protected ScrollRect _scrollRect;
    [ReadOnly] public List<TData> Cells;

    protected RectTransform _contentPanel;
    protected int _totalItems;
    protected float _itemHeight;
    protected List<RectTransform> _visibleItems = new List<RectTransform>();
    protected int _itemsPerScreen;
    protected int _firstVisibleItemIndex;

    protected virtual void Start()
    {
        if (_scrollRect == null)
            _scrollRect = GetComponent<ScrollRect>();
        _contentPanel = _scrollRect.content;
    }
    public void SnapToIndex(int index)
    {
        if (index < 0 || index >= _totalItems)
        {
            Debug.LogWarning($"SnapToIndex: Index {index} out of range [0, {_totalItems - 1}]");
            return;
        }

        float contentHeight = _scrollRect.content.sizeDelta.y;
        float viewportHeight = _scrollRect.viewport.rect.height;

        float itemY = index * (_itemHeight + _spacing);

        float maxScroll = Mathf.Max(0, contentHeight - viewportHeight);
        float targetScroll = Mathf.Clamp(itemY, 0, maxScroll);

        float normalized = maxScroll == 0 ? 1f : (targetScroll / maxScroll);
        if (float.IsNaN(normalized)) normalized = 1f;

        _scrollRect.verticalNormalizedPosition = normalized;
        Canvas.ForceUpdateCanvases();
    }

    public void StartInit()
    {
        if (Cells.Count > 0)
        {
            _totalItems = Cells.Count;
            _itemHeight = _itemPrefab.GetComponent<RectTransform>().sizeDelta.y;
            InitializeScrollRect();
            PopulateInitialItems();
        }
    }

    protected void InitializeScrollRect()
    {
        _scrollRect.onValueChanged.AddListener(OnScrollValueChanged);

        // Tính toán số item hiển thị trên màn hình với padding
        float availableHeight = _scrollRect.GetComponent<RectTransform>().rect.height - _padding.top - _padding.bottom;
        _itemsPerScreen = Mathf.Min(Mathf.CeilToInt(availableHeight / (_itemHeight + _spacing)) + 3, Cells.Count);

        // Tính toán content size với padding
        float totalContentHeight = _padding.top + _padding.bottom + (_totalItems * _itemHeight) + ((_totalItems - 1) * _spacing);
        if (_contentPanel == null)
            _contentPanel = _scrollRect.content;
        _contentPanel.sizeDelta = new Vector2(_contentPanel.sizeDelta.x, totalContentHeight);
    }

    protected void PopulateInitialItems()
    {
        for (int i = 0; i < _itemsPerScreen; i++)
        {
            if (i < _totalItems)
            {
                GameObject item = Instantiate(_itemPrefab, _contentPanel.transform);
                RectTransform rectTransform = item.GetComponent<RectTransform>();
                SetupItemTransform(rectTransform, i);
                _visibleItems.Add(rectTransform);
                UpdateItemContent(rectTransform, Cells[i], i);
            }
        }
    }

    protected void SetupItemTransform(RectTransform rectTransform, int index)
    {
        // Đặt anchor và pivot về giữa trên (giữ nguyên size gốc)
        rectTransform.anchorMin = new Vector2(0.5f, 1f);
        rectTransform.anchorMax = new Vector2(0.5f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 1f);

        // Không thay đổi sizeDelta

        // Tính toán vị trí y dựa trên padding và spacing
        float yPosition = -_padding.top - (index * (_itemHeight + _spacing));
        float xPosition = 0; // Giữa content

        rectTransform.anchoredPosition = new Vector2(xPosition, yPosition);
    }

    /// <summary>
    /// Abstract method để subclass implement cách update nội dung item
    /// </summary>
    /// <param name="item">RectTransform của item cần update</param>
    /// <param name="data">Data để hiển thị</param>
    /// <param name="index">Index trong danh sách Cells</param>
    protected abstract void UpdateItemContent(RectTransform item, TData data, int index);

    protected void OnScrollValueChanged(Vector2 value)
    {
        float newScrollPosition = _contentPanel.anchoredPosition.y;
        int newFirstVisibleItemIndex = Mathf.Clamp(Mathf.FloorToInt((newScrollPosition - _padding.top) / (_itemHeight + _spacing)), 0, _totalItems - _itemsPerScreen);

        if (newFirstVisibleItemIndex != _firstVisibleItemIndex)
        {
            UpdateVisibleItems(newFirstVisibleItemIndex);
        }
    }

    public void UpdateVisibleItems(int newFirstVisibleItemIndex)
    {
        int itemsToMove = Mathf.Abs(newFirstVisibleItemIndex - _firstVisibleItemIndex);
        bool scrollingDown = newFirstVisibleItemIndex > _firstVisibleItemIndex;

        for (int i = 0; i < itemsToMove; i++)
        {
            if (scrollingDown)
            {
                if (_firstVisibleItemIndex + _itemsPerScreen < _totalItems)
                {
                    MoveItemToBottom();
                }
            }
            else
            {
                if (_firstVisibleItemIndex > 0)
                {
                    MoveItemToTop();
                }
            }
        }
        _firstVisibleItemIndex = newFirstVisibleItemIndex;
    }

    protected void MoveItemToBottom()
    {
        RectTransform item = _visibleItems[0];
        _visibleItems.RemoveAt(0);
        _visibleItems.Add(item);

        int newIndex = _firstVisibleItemIndex + _itemsPerScreen;
        SetupItemTransform(item, newIndex);
        UpdateItemContent(item, Cells[newIndex], newIndex);

        _firstVisibleItemIndex++;
    }

    protected void MoveItemToTop()
    {
        RectTransform item = _visibleItems[_visibleItems.Count - 1];
        _visibleItems.RemoveAt(_visibleItems.Count - 1);
        _visibleItems.Insert(0, item);

        _firstVisibleItemIndex--;
        SetupItemTransform(item, _firstVisibleItemIndex);
        UpdateItemContent(item, Cells[_firstVisibleItemIndex], _firstVisibleItemIndex);
    }

    public void Despawn()
    {
        _scrollRect.onValueChanged.RemoveListener(OnScrollValueChanged);
        _firstVisibleItemIndex = 0;
        _scrollRect.StopMovement();
        foreach (var item in _visibleItems)
        {
            Destroy(item.gameObject);
        }
        Cells.Clear();
        _visibleItems.Clear();
    }
}
