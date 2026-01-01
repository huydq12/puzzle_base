
public class VibrateManager : Singleton<VibrateManager>
{
    private bool _vibrateEnabled = true;
    private new void Awake()
    {
        base.Awake();
        // Vibration.Initialize();

        Game.Launch();
        var userData = Game.Data.Load<UserData>();
        if (userData != null)
        {
            SetVibrateEnabled(userData.vibrateOn);
        }
    }
    public void SmallVibrate()
    {
        if (!_vibrateEnabled) return;
        // Vibration.Pop();
    }
    public void MediumVibrate()
    {
        if (!_vibrateEnabled) return;
        // Vibration.Peek();
    }

    public void SetVibrateEnabled(bool enabled)
    {
        _vibrateEnabled = enabled;
    }

    public bool IsVibrateEnabled() => _vibrateEnabled;
}
