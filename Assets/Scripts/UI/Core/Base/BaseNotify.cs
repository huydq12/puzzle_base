public abstract class BaseNotify : UIElement
{
    public override bool ManualHide => false;
    public override bool DestroyOnHide => false;
    public override bool UseBehindPanel => false;

    public override void Init()
    {
        base.Init();
        SetUIType(UIType.Notify);
    }

    protected override void Awake()
    {
        SetUIType(UIType.Notify);
        base.Awake();
    }
}
