
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
public enum Direction
{
    Forward = 0,
    Right = 1,
    Back = 2,
    Left = 3
}
public enum CubeType
{
    Normal,
    Corner,
    Head
}
public class CubeLine : SerializedMonoBehaviour
{
    public static int JumpingToHoleCount = 0;
    [ReadOnly] public Line Line;
    [ReadOnly] public CubeType Type;
    [ReadOnly] public ObjectColor Color;
    [ReadOnly] public int ElementType;
    [ReadOnly] public GridCell Cell;
    [SerializeField] private ParticleSystem _hitEffect;
    [SerializeField] private ParticleSystem _wariningEffect;
    [SerializeField] private ParticleSystem _warningHeadEffect;
    [SerializeField] private ParticleSystem _meltEffect;
    [OdinSerialize] private Dictionary<CubeType, Renderer> _renderers;
    [SerializeField] private SpriteRenderer _head;
    [SerializeField] private Renderer _doubleCube;
    [SerializeField] private Renderer _doubleHeadCube;
    [SerializeField] private UnityEngine.Color _headNormalColor = UnityEngine.Color.white;
    [SerializeField] private UnityEngine.Color _headHighlightColor = new UnityEngine.Color(1f, 1f, 1f, 1f);
    [SerializeField] private Material _materialElementType2;
    [SerializeField] private Collider _collider;
    [SerializeField] private float _holeLandingYOffset = 0.22f;
    private Quaternion _initRotation;
    private bool _elementType3Revealed;
    private ObjectColor _baseColor;
    private ObjectColor _originalColor;
    private bool _hasElementType3InnerColor;
    private ObjectColor _elementType3InnerColor;
    private bool _isHeadHighlighted;

    public bool Cantouch
    {
        get => _collider.enabled;
        set => _collider.enabled = value;
    }
    public bool HighlightHead
    {
        get => _isHeadHighlighted;
        set
        {
            _isHeadHighlighted = value;
            UpdateHeadHighlightColor();
        }
    }
    public bool IsElementType3Revealed => _elementType3Revealed;
    public ObjectColor OriginalColor => _originalColor;

    public void SetElementType3InnerColor(ObjectColor innerColor)
    {
        _hasElementType3InnerColor = innerColor != ObjectColor.None;
        _elementType3InnerColor = innerColor;
        if (ElementType == 3)
        {
            RefreshColorAndMaterials(Type);
        }
    }
    public void ShowWarning()
    {
        _warningHeadEffect.Stop();
        _wariningEffect.Stop();
        if (Type == CubeType.Head)
        {
            _warningHeadEffect.Play();
        }
        else
        {
            _wariningEffect.Play();
        }
    }
    public bool BringToTop
    {
        set
        {
            foreach (var pair in _renderers)
            {
                Common.SetLayerRecursively(pair.Value.gameObject, value ? LayerMask.NameToLayer("Top") : LayerMask.NameToLayer("Cube"));
            }
        }
    }
    private bool _isJumpingToHole;

    private void OnDestroy()
    {
        if (_isJumpingToHole)
        {
            JumpingToHoleCount--;
            _isJumpingToHole = false;
        }
    }

    public bool OnHitByHole(bool byRainbow, Vector3 peakPosition, Transform holeBottom, System.Action onArrived = null)
    {
        if (ElementType == 2 && Line != null && Line.IsIceLine && Line.RemainingCounter > 0)
            return false;

        if (ElementType == 3 && !_elementType3Revealed)
        {
            if (byRainbow)
                ShooterController.Instance.ReduceShooterTotalByColor(Color, 1);

            // First hole hit: peel the currently visible layer into the hole.
            SpawnElementType3InnerLayerJumpToHole(peakPosition, holeBottom);
            HideDoubleLayerVisuals();

            _elementType3Revealed = true;
            // After peeling one layer, the remaining cube should switch to inner color.
            if (!TryGetElementType3ShiftedColorFromOriginal(offset: 3, out ObjectColor shifted))
                shifted = _originalColor;
            _baseColor = shifted;
            RefreshColorAndMaterials(Type);
            SpawnHitEffect();
            // Type 3 should peel one layer per hit. First hit only reveals inner layer.
            return false;
        }

        Cantouch = false;
        if (byRainbow)
            ShooterController.Instance.ReduceShooterTotalByColor(Color, 1);

        ConveyorController.Instance.RemoveCubeFromPath(this);
        JumpToHole(peakPosition, holeBottom, onArrived);
        return true;
    }

    private void SpawnElementType3InnerLayerJumpToHole(Vector3 peakPosition, Transform holeBottom)
    {
        Transform source = GetElementType3InnerLayerVisualSource();
        if (source == null) return;

        GameObject ghost = Instantiate(source.gameObject, source.position, source.rotation);
        if (ghost == null) return;

        ghost.transform.SetParent(null, true);
        ghost.transform.localScale = source.lossyScale;
        StripGhostToSingleVisual(source, ghost.transform);
        Common.SetLayerRecursively(ghost, LayerMask.NameToLayer("Top"));

        var colliders = ghost.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;

        ghost.transform.DOKill();

        Vector3 startPos = ghost.transform.position;
        Vector3 endPos = holeBottom != null
            ? holeBottom.position + Vector3.up * _holeLandingYOffset
            : peakPosition;
        Vector3 startScale = ghost.transform.localScale;

        float horizontalDistance = Vector2.Distance(
            new Vector2(startPos.x, startPos.z),
            new Vector2(endPos.x, endPos.z)
        );
        float travelDuration = Mathf.Lerp(0.28f, 0.42f, Mathf.InverseLerp(0.5f, 6f, horizontalDistance));
        float dynamicApex = Mathf.Max(startPos.y, endPos.y) + Mathf.Clamp(horizontalDistance * 0.32f, 0.45f, 1.5f);
        float apexY = Mathf.Max(dynamicApex, peakPosition.y);
        float baselineMidY = (startPos.y + endPos.y) * 0.5f;
        float arcHeight = Mathf.Max(0.08f, apexY - baselineMidY);

        Sequence seq = DOTween.Sequence();
        seq.Append(
            DOVirtual.Float(0f, 1f, travelDuration, t =>
            {
                if (ghost == null) return;
                Vector3 pos = Vector3.Lerp(startPos, endPos, t);
                pos.y = Mathf.Lerp(startPos.y, endPos.y, t) + arcHeight * 4f * t * (1f - t);
                ghost.transform.position = pos;
            }).SetEase(Ease.InQuad)
        );
        seq.Join(
            ghost.transform.DOScale(startScale * 0.25f, travelDuration * 0.55f)
                .SetDelay(travelDuration * 0.45f)
                .SetEase(Ease.InQuad)
        );
        seq.Append(ghost.transform.DOScale(0f, 0.06f).SetEase(Ease.OutQuad));
        seq.OnComplete(() =>
        {
            if (ghost != null)
                Destroy(ghost);
        });
    }

    private Transform GetElementType3InnerLayerVisualSource()
    {
        // Use currently visible main layer first so the first-hole jump keeps the hit color.
        if (_renderers != null && _renderers.TryGetValue(Type, out Renderer renderer) && renderer != null && renderer.enabled)
            return renderer.transform;

        if (_head != null && _head.enabled)
            return _head.transform;

        if (Type == CubeType.Head)
        {
            if (_doubleHeadCube != null && _doubleHeadCube.gameObject.activeInHierarchy)
                return _doubleHeadCube.transform;
        }
        else
        {
            if (_doubleCube != null && _doubleCube.gameObject.activeInHierarchy)
                return _doubleCube.transform;
        }

        return null;
    }

    private static void StripGhostToSingleVisual(Transform source, Transform ghostRoot)
    {
        if (source == null || ghostRoot == null) return;

        Renderer sourceRenderer = source.GetComponent<Renderer>();
        if (sourceRenderer != null)
        {
            int sourceRendererIndex = -1;
            var sourceRootRenderers = source.GetComponents<Renderer>();
            for (int i = 0; i < sourceRootRenderers.Length; i++)
            {
                if (sourceRootRenderers[i] == sourceRenderer)
                {
                    sourceRendererIndex = i;
                    break;
                }
            }

            var ghostRootRenderers = ghostRoot.GetComponents<Renderer>();
            for (int i = 0; i < ghostRootRenderers.Length; i++)
            {
                if (ghostRootRenderers[i] == null) continue;
                bool keepThisRenderer = sourceRendererIndex >= 0
                    ? i == sourceRendererIndex
                    : i == 0;
                ghostRootRenderers[i].enabled = keepThisRenderer;
            }
        }

        var childRenderers = ghostRoot.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < childRenderers.Length; i++)
        {
            if (childRenderers[i] == null) continue;
            if (childRenderers[i].transform != ghostRoot)
                childRenderers[i].enabled = false;
        }
    }

    private void HideDoubleLayerVisuals()
    {
        if (_doubleCube != null)
            _doubleCube.gameObject.SetActive(false);
        if (_doubleHeadCube != null)
            _doubleHeadCube.gameObject.SetActive(false);
    }

    private void JumpToHole(Vector3 peakPosition, Transform holeBottom, System.Action onArrived)
    {
        _isJumpingToHole = true;
        JumpingToHoleCount++;
        transform.DOKill();

        Vector3 startPos = transform.position;
        Vector3 endPos = holeBottom != null
            ? holeBottom.position + Vector3.up * _holeLandingYOffset
            : peakPosition;
        Vector3 startScale = transform.localScale;

        float horizontalDistance = Vector2.Distance(
            new Vector2(startPos.x, startPos.z),
            new Vector2(endPos.x, endPos.z)
        );
        float travelDuration = Mathf.Lerp(0.32f, 0.48f, Mathf.InverseLerp(0.5f, 6f, horizontalDistance));
        float dynamicApex = Mathf.Max(startPos.y, endPos.y) + Mathf.Clamp(horizontalDistance * 0.35f, 0.6f, 1.8f);
        float apexY = Mathf.Max(dynamicApex, peakPosition.y);
        float baselineMidY = (startPos.y + endPos.y) * 0.5f;
        float arcHeight = Mathf.Max(0.1f, apexY - baselineMidY);

        Sequence seq = DOTween.Sequence();
        Vector3 anticipationScale = Vector3.Scale(startScale, new Vector3(1.06f, 0.9f, 1.06f));
        seq.Append(transform.DOScale(anticipationScale, 0.05f).SetEase(Ease.OutQuad));
        seq.Append(transform.DOScale(startScale, 0.06f).SetEase(Ease.OutQuad));
        seq.Append(
            DOVirtual.Float(0f, 1f, travelDuration, t =>
            {
                Vector3 pos = Vector3.Lerp(startPos, endPos, t);
                pos.y = Mathf.Lerp(startPos.y, endPos.y, t) + arcHeight * 4f * t * (1f - t);
                transform.position = pos;
            }).SetEase(Ease.InQuad)
        );
        seq.Append(transform.DOScale(startScale * 0.28f, 0.08f).SetEase(Ease.InQuad));
        seq.Append(transform.DOScale(0f, 0.07f).SetEase(Ease.OutQuad));

        seq.OnComplete(() =>
        {
            _isJumpingToHole = false;
            JumpingToHoleCount--;
            onArrived?.Invoke();
            Destroy(gameObject);
        });
    }

    public bool OnHit(bool byRainbow = false)
    {
        if (ElementType == 2 && Line != null && Line.IsIceLine && Line.RemainingCounter > 0)
            return false;

        if (ElementType == 3 && !_elementType3Revealed)
        {
            if (byRainbow)
            {
                // Rainbow shots should still "pay" for the current visible layer color.
                ShooterController.Instance.ReduceShooterTotalByColor(Color, 1);
            }

            _elementType3Revealed = true;
            if (!TryGetElementType3ShiftedColorFromOriginal(offset: 3, out ObjectColor shifted))
                shifted = _originalColor;
            _baseColor = shifted;
            RefreshColorAndMaterials(Type);

            SpawnHitEffect();

            return false;
        }
        Cantouch = false;
        if (byRainbow)
        {
            ShooterController.Instance.ReduceShooterTotalByColor(Color, 1);
        }

        ConveyorController.Instance.RemoveCubeFromPath(this);
        SpawnHitEffect();
        transform.DOScale(0f, 0.1f).OnComplete(() => Destroy(gameObject));
        return true;
    }

    private void SpawnHitEffect()
    {
        if (_hitEffect == null) return;

        ParticleSystem ps = Instantiate(_hitEffect, transform.position, Quaternion.identity);

        if (ps == null) return;

        ps.transform.rotation = Quaternion.identity;
        ps.Play(true);

        var main = ps.main;
        float lifetime = 0f;
        if (main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants)
            lifetime = main.startLifetime.constantMax;
        else
            lifetime = main.startLifetime.constant;

        float delay = Mathf.Max(0.1f, main.duration + lifetime);
        DOVirtual.DelayedCall(delay, () =>
        {
            if (ps != null)
                Destroy(ps.gameObject);
        });
    }

    private void UpdateHeadHighlightColor()
    {
        if (_head == null) return;
        bool shouldHighlight = _head.enabled && _isHeadHighlighted;
        _head.color = shouldHighlight ? _headHighlightColor : _headNormalColor;
    }

    private void ApplyHeadColor()
    {
        if (_head == null) return;
        UpdateHeadHighlightColor();
    }

    public void SetColor(ObjectColor color)
    {
        _originalColor = color;
        _baseColor = color;
        RefreshColorAndMaterials(Type);
    }

    public void SetElementType(int elementType)
    {
        _elementType3Revealed = false;
        _hasElementType3InnerColor = false;
        _elementType3InnerColor = ObjectColor.None;
        ElementType = elementType;
        RefreshColorAndMaterials(Type);
        Line?.RefreshCounterText();
    }
    public void PlayEffectMelt()
    {
        if (_meltEffect == null) return;
        _meltEffect.Play();
    }

    private Material GetElementTypeMaterial()
    {
        if (ElementType == 2) return _materialElementType2;
        return null;
    }

    private static int GetObjectColorCount()
    {
        return System.Enum.GetValues(typeof(ObjectColor)).Length;
    }

    private static readonly ObjectColor[] ElementType3ColorIndexOrder =
    {
        ObjectColor.Green,
        ObjectColor.Blue,
        ObjectColor.Red,
        ObjectColor.Purple,
        ObjectColor.Pink,
        ObjectColor.Yellow,
        ObjectColor.Orange,
        ObjectColor.Cyan,
        ObjectColor.Brown,
        ObjectColor.Teal,
        ObjectColor.Black,
        ObjectColor.White,
    };

    private bool TryGetElementType3ShiftedColorFromOriginal(int offset, out ObjectColor shiftedColor)
    {
        if (_hasElementType3InnerColor)
        {
            shiftedColor = _elementType3InnerColor;
            return shiftedColor != ObjectColor.None && shiftedColor != _originalColor;
        }

        int baseIdx = System.Array.IndexOf(ElementType3ColorIndexOrder, _originalColor);
        if (baseIdx < 0)
        {
            shiftedColor = _originalColor;
            return false;
        }

        int idx = baseIdx - offset;
        if (idx < 0)
        {
            // Out-of-range for the usual backward shift (baseIdx < offset):
            // shift forward instead so the 2-layer cube always changes to a different color.
            int forwardIdx = baseIdx + (offset + 1);
            if (forwardIdx >= 0 && forwardIdx < ElementType3ColorIndexOrder.Length)
            {
                shiftedColor = ElementType3ColorIndexOrder[forwardIdx];
                return shiftedColor != _originalColor;
            }

            shiftedColor = _originalColor;
            return false;
        }
        if (idx >= ElementType3ColorIndexOrder.Length)
        {
            shiftedColor = _originalColor;
            return false;
        }

        shiftedColor = ElementType3ColorIndexOrder[idx];
        return shiftedColor != _originalColor;
    }

    private void RefreshColorAndMaterials(CubeType targetType)
    {
        Color = _baseColor;
        ApplyMaterials(targetType);
    }

    private void ApplyMaterials(CubeType targetType)
    {
        if (ElementType == 3)
        {
            ApplyElementType3Materials(targetType);
            return;
        }

        Material overrideMat = GetElementTypeMaterial();

        Material cubeMat = overrideMat;

        if (cubeMat == null)
        {
            if (Board.Instance == null || Board.Instance.ColorConfig == null) return;
            cubeMat ??= Board.Instance.ColorConfig.GetCubeColor(Color);
        }

        if (_renderers != null && cubeMat != null)
        {
            foreach (var renderer in _renderers.Values)
            {
                if (renderer == null) continue;
                renderer.sharedMaterial = cubeMat;
            }
        }

        ApplyHeadColor();

        if (_doubleCube != null) _doubleCube.gameObject.SetActive(false);
        if (_doubleHeadCube != null) _doubleHeadCube.gameObject.SetActive(false);
    }

    private void ApplyElementType3Materials(CubeType targetType)
    {
        if (Board.Instance == null || Board.Instance.ColorConfig == null) return;

        Material outerCubeMat = Board.Instance.ColorConfig.GetCubeColor(_baseColor);
        ObjectColor overlayColor = _originalColor;
        if (!TryGetElementType3ShiftedColorFromOriginal(offset: 3, out overlayColor))
            overlayColor = _originalColor;

        Material overlayCubeMat = Board.Instance.ColorConfig.GetCubeColor(overlayColor);
        Material overlayHeadMat = Board.Instance.ColorConfig.GetCubeHeadColor(overlayColor);

        if (_renderers != null && outerCubeMat != null)
        {
            foreach (var renderer in _renderers.Values)
            {
                if (renderer == null) continue;
                renderer.sharedMaterial = outerCubeMat;
            }
        }

        ApplyHeadColor();

        if (_doubleCube != null)
        {
            if (!_elementType3Revealed && overlayCubeMat != null)
                _doubleCube.sharedMaterial = overlayCubeMat;
            _doubleCube.gameObject.SetActive(!_elementType3Revealed && targetType != CubeType.Head);
        }

        if (_doubleHeadCube != null)
        {
            if (!_elementType3Revealed && targetType == CubeType.Head)
            {
                if (overlayHeadMat != null)
                    _doubleHeadCube.sharedMaterial = overlayHeadMat;
                _doubleHeadCube.gameObject.SetActive(true);
            }
            else
            {
                _doubleHeadCube.gameObject.SetActive(false);
            }
        }
    }
    public void RevertType()
    {
        transform.rotation = _initRotation;
        RefreshColorAndMaterials(Type);
        if (_head != null) _head.enabled = Type == CubeType.Head;
        foreach (var pair in _renderers)
        {
            bool enable = pair.Key == Type;
            pair.Value.enabled = enable;
        }
        UpdateHeadHighlightColor();
    }
    public void SetTempType(CubeType type)
    {
        _initRotation = transform.rotation;
        transform.rotation = Quaternion.identity;
        if (_head != null) _head.enabled = type == CubeType.Head;
        RefreshColorAndMaterials(type);
        foreach (var pair in _renderers)
        {
            bool enable = pair.Key == type;
            pair.Value.enabled = enable;
        }
        UpdateHeadHighlightColor();
    }
    public void SetType(CubeType type)
    {
        Type = type;
        if (_head != null) _head.enabled = type == CubeType.Head;
        RefreshColorAndMaterials(type);
        foreach (var pair in _renderers)
        {
            bool enable = pair.Key == type;
            pair.Value.enabled = enable;
        }
        UpdateHeadHighlightColor();
    }
    public void Clear()
    {
        DOTween.Kill(transform, false);

        Line = null;
        Cell = null;

        Type = CubeType.Normal;
        Color = ObjectColor.Green;
        ElementType = 0;
        _elementType3Revealed = false;
        _baseColor = ObjectColor.Green;
        _originalColor = ObjectColor.Green;
        _hasElementType3InnerColor = false;
        _elementType3InnerColor = ObjectColor.None;

        transform.localScale = Vector3.one;
        transform.rotation = Quaternion.identity;
        _initRotation = Quaternion.identity;

        if (_wariningEffect != null)
            _wariningEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (_warningHeadEffect != null)
            _warningHeadEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        if (_meltEffect != null)
            _meltEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        Cantouch = true;
        HighlightHead = false;
        BringToTop = false;

        if (_renderers != null)
        {
            foreach (var pair in _renderers)
            {
                if (pair.Value != null)
                    pair.Value.enabled = false;
            }
        }

        if (_head != null)
            _head.enabled = false;

        if (_doubleCube != null)
            _doubleCube.gameObject.SetActive(false);

        if (_doubleHeadCube != null)
            _doubleHeadCube.gameObject.SetActive(false);
    }
}
