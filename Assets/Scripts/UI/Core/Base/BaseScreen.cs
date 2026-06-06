public abstract class BaseScreen : UIElement
{
    public override bool ManualHide => false;
    public override bool DestroyOnHide => false;
    public override bool UseBehindPanel => false;

    public override void Init()
    {
        base.Init();
        SetUIType(UIType.Screen);
    }

    protected override void Awake()
    {
        SetUIType(UIType.Screen);
        base.Awake();
    }
}
