using DG.Tweening;
using TMPro;
using UnityEngine;

public class SettingsPopup : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private TMP_InputField _inputCheat;

    void Awake()
    {
        _inputCheat.onSubmit.AddListener(delegate
        {
            if (int.TryParse(_inputCheat.text, out int result))
            {
                GameManagerInGame.Instance.StartGame(result);
                Hide();
            }
        });
    }
    public void OnContinueClick()
    {
        Hide();
    }
    public void OnRestartClick()
    {
        DOTween.KillAll();
        Hide();
        GameManagerInGame.Instance.StartGame();
    }
    public void Show()
    {
        if (GameManagerInGame.Instance.CurrentGameStateInGame == GameStateInGame.Playing)
        {
            GameManagerInGame.Instance.SetState(GameStateInGame.Pause);
        }
        gameObject.SetActive(true);
        _canvasGroup.blocksRaycasts = true;
        _canvasGroup.DOFade(1, 0.25f);
    }
    public void Hide()
    {
        if (GameManagerInGame.Instance.CurrentGameStateInGame == GameStateInGame.Pause)
        {
            GameManagerInGame.Instance.SetState(GameStateInGame.Playing);
        }
        _canvasGroup.DOFade(0, 0.25f).OnComplete(() =>
        {
            gameObject.SetActive(false);
            _canvasGroup.blocksRaycasts = false;
        });
    }
}
