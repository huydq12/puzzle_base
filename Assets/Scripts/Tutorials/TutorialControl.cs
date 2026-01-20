using UnityEngine;

public class TutorialControl : TutorialBase
{
    [Header("Arrow (3D)")]
    [SerializeField] private GameObject arrow;
    [SerializeField] private Transform arrowTransform;
    [SerializeField] private Vector3 arrowOffset;

    [Header("Target (3D)")]
    [SerializeField] private Collider targetCollider;
    [SerializeField] private Camera raycastCamera;
    [SerializeField] private LayerMask raycastMask = ~0;
    [SerializeField] private float raycastMaxDistance = 1000f;

    private bool _listening;

    public override void Setup()
    {
        base.Setup();
        Type = TutorialType.Control;
        _tutName = Type.ToString();
    }

    private void OnEnable()
    {
        StartListening();
        RefreshArrowPosition();
    }

    private void OnDisable()
    {
        StopListening();
    }

    private void StartListening()
    {
        if (_listening) return;
        _listening = true;
    }

    private void StopListening()
    {
        if (!_listening) return;
        _listening = false;
    }

    public override void Show()
    {
        base.Show();
        if (arrow != null) arrow.SetActive(true);
        RefreshArrowPosition();
    }

    public override void Hide()
    {
        if (arrow != null) arrow.SetActive(false);
        base.Hide();
    }

    private void Update()
    {
        if (!_listening) return;

        if (targetCollider == null) return;

        if (TryGetPointerDown(out Vector2 screenPos))
        {
            Camera cam = raycastCamera != null ? raycastCamera : Camera.main;
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, raycastMaxDistance, raycastMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider == targetCollider)
                {
                    OnCorrectClick();
                }
            }
        }
    }

    private static bool TryGetPointerDown(out Vector2 screenPos)
    {
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began)
            {
                screenPos = t.position;
                return true;
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            screenPos = Input.mousePosition;
            return true;
        }

        screenPos = default;
        return false;
    }

    private void RefreshArrowPosition()
    {
        if (arrowTransform == null) arrowTransform = arrow != null ? arrow.transform : null;
        if (arrowTransform == null) return;

        if (targetCollider != null)
        {
            arrowTransform.position = targetCollider.transform.position + arrowOffset;
        }
    }

    private void OnCorrectClick()
    {
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.TutorialFinish();
        }

        Hide();
    }
}
