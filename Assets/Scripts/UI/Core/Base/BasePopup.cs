public abstract class BasePopup : UIElement
{
    public override bool ManualHide => true;
    public override bool DestroyOnHide => false;
    public override bool UseBehindPanel => false;

    public override void Init()
    {
        base.Init();
        SetUIType(UIType.Popup);
    }

    protected override void Awake()
    {
        SetUIType(UIType.Popup);
        base.Awake();
    }
}
