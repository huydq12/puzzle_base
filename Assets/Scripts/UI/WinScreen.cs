using DG.Tweening;
using UnityEngine;

public class WinScreen : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    public void OnContinueClick()
    {
        DOTween.KillAll();
        Hide();
        GameManagerInGame.Instance.StartGame();
    }
    public void Show()
    {
        gameObject.SetActive(true);
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.DOFade(1, 0.25f);
    }
    public void Hide()
    {
        _canvasGroup.DOFade(0, 0.25f).OnComplete(() =>
        {
            gameObject.SetActive(false);
            _canvasGroup.blocksRaycasts = false;
        });
    }
}
