
public class TutorialLockItem : TutorialBase
{
    public override void Setup()
    {
        base.Setup();
        Type = TutorialType.LockItem;
        _tutName = Type.ToString();
    }

    public override async void GoNextStep()
    {
        base.GoNextStep();

        if (_currentStep == 1)
        {
            Show();
            GameUI.Instance.Get<UITutorial>().Show();
            GoNextStep();
        }
        else
        {
            if (IsFinish())
            {
                TutorialManager.Instance.TutorialFinish();
                Hide();
            }
        }
    }

    public override bool IsFinish()
    {
        if (_currentStep > 1)
        {
            return true;
        }
        return false;
    }
}
