using UnityEngine;

public class TutorialBase : MonoBehaviour
{
    [HideInInspector]
    public TutorialType Type;
    protected string _tutName;
    protected int _currentStep = 0;

    public virtual void Setup()
    {
        _currentStep = 0;
    }

    public virtual void GoNextStep()
    {
        _currentStep++;
    }

    public virtual void Hide()
    {
        gameObject.SetActive(false);
    }

    public virtual void Show()
    {
        gameObject.SetActive(true);
    }

    public virtual bool IsFinish()
    {
        return false;
    }
}

