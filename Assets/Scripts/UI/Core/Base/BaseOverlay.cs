public abstract class BaseOverlay : UIElement
{
    public override bool ManualHide => false;
    public override bool DestroyOnHide => false;
    public override bool UseBehindPanel => false;

    public override void Init()
    {
        base.Init();
        SetUIType(UIType.Overlap);
    }

    protected override void Awake()
    {
        SetUIType(UIType.Overlap);
        base.Awake();
    }
}
