using DG.Tweening;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;
using Sequence = DG.Tweening.Sequence;
public enum ObjectColor
{
    Green,
    Red,
    Blue,
    Purple,
    Pink,
    Yellow,
    Orange,
    Cyan,
    Brown,
    Teal,
}
public class ButtonCircle : MonoBehaviour
{
    [SerializeField] private Renderer _renderer;
    [ReadOnly] public ObjectColor Color;
    [ReadOnly] public ButtonStack Stack;
    [SerializeField] private MeshCollider _collider;
    public Renderer Renderer => _renderer;
    void Awake()
    {
        _collider.enabled = false;
    }
    public void SetColor(ObjectColor color)
    {
        Color = color;
        _renderer.material = Board.Instance.ColorConfig.GetButtonByColor(color);
    }
    public void Configure(ButtonStack stack)
    {
        Stack = stack;
        if (stack != null)
        {
            transform.SetParent(stack.transform);
        }
        else transform.SetParent(null);
    }
    [Button]
    public Sequence MoveToContainer(Container container)
    {
        Sequence sq = DOTween.Sequence();
        if (container == null)
        {
            sq.AppendCallback(delegate { });
            return sq;
        }

        var slot = container.NextEmptySlot();
        if (slot == null)
        {
            sq.AppendCallback(delegate { });
            return sq;
        }

        if (Board.Instance != null)
        {
            Board.Instance.OnCollected(Color, 1);
        }
        if (Stack != null)
        {
            Stack.RemoveButton(this);
        }
        slot.ButtonInSlot = this;
        transform.SetParent(slot.transform);
        sq.Join(transform.DORotate(Vector3.zero, 0.5f));
        sq.Join(transform.DOScale(0.75f, 0.5f));
        sq.Join(transform.DOLocalMove(Vector3.zero, 0.5f));
        sq.AppendCallback(delegate
        {
            var rb = transform.AddComponent<Rigidbody>();
            rb.mass = 2.5f;
            rb.drag = 1f;
            rb.angularDrag = 1f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _collider.enabled = true;
        });
        sq.AppendInterval(0.7f);
        return sq;
    }
}
