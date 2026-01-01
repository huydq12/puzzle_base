using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

public class LoseScreen : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;

    public void OnRestartClick()
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
