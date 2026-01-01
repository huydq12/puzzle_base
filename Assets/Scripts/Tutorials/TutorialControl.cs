using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;

public class TutorialControl : TutorialBase
{
    [SerializeField] private RectTransform _canvasRectTransform;
    [SerializeField] private RectTransform _hand;
    [SerializeField] private GameObject _arrowStep1;
    [SerializeField] private GameObject _arrowStep2;
    [SerializeField] private TextMeshProUGUI _tapText;
    [SerializeField] private CanvasGroup _tapTextCanvasGroup;
    [SerializeField] private CanvasGroup _handCanvasGroup;

    public override void Setup()
    {
        base.Setup();
        Type = TutorialType.Control;
        _tutName = Type.ToString();
        GameManagerInGame.Instance.OnEndLevel += OnEndLevel;
        GameManagerInGame.Instance.OnStartLevel += OnStartLevel;
    }
    private void OnStartLevel()
    {
        Hide();
        GameManagerInGame.Instance.OnStartLevel -= OnStartLevel;
    }

    private void OnEndLevel()
    {
        _tapTextCanvasGroup.DOFade(0, 0.5f).OnComplete(() =>
        {
            Hide();
            GameManagerInGame.Instance.OnEndLevel -= OnEndLevel;
        });
    }

    public override void GoNextStep()
    {
        base.GoNextStep();
        if (_currentStep == 1)
        {
            Show();
        }
        StartCoroutine(GoNextStepCoroutine());
        IEnumerator GoNextStepCoroutine()
        {

            switch (_currentStep)
            {
                case 1:
                    {
                        _handCanvasGroup.DOKill();
                        _tapTextCanvasGroup.DOKill();
                        _hand.DOKill();

                        _handCanvasGroup.alpha = 0f;
                        _tapTextCanvasGroup.alpha = 0f;
                        yield return new WaitForSeconds(0.2f);
                        PlayMoveHand();
                        PlayShowText("Drag matching buttons next to each other");
                        yield return new WaitForSeconds(0.5f);
                        TutorialManager.Instance.TutorialControlWaitMoveButton = true;
                        break;
                    }
                default:
                    {
                        if (IsFinish())
                        {
                            TutorialManager.Instance.TutorialControlWaitMoveButton = false;
                            TutorialManager.Instance.TutorialFinish();
                            float dur = 0.5f;
                            _arrowStep1.SetActive(false);
                            _arrowStep2.SetActive(true);
                            _tapTextCanvasGroup.gameObject.SetActive(false);
                            _hand.DOKill();
                            _handCanvasGroup.DOKill();
                            _tapTextCanvasGroup.DOKill();
                            _handCanvasGroup.DOFade(0f, dur);
                        }
                        break;
                    }
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

    private void PlayMoveHand()
    {
        float fadeDur = 0.5f;
        float moveDur = 0.7f;

        GridCell startCell = Board.Instance.CellStartInTutorialControl();
        GridCell endCell = Board.Instance.CellsEndInTutorialControl();

        if (startCell != null && endCell != null)
        {
            Vector3 screenPosStart = Camera.main.WorldToScreenPoint(startCell.transform.position + new Vector3(0f, 0f, 0f));
            Vector3 screenPosEnd = Camera.main.WorldToScreenPoint(endCell.transform.position + new Vector3(0f, 0f, 0f));

            Vector2 localPointStartInCanvas;
            Vector2 localPointEndInCanvas;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRectTransform,
                new Vector2(screenPosStart.x, screenPosStart.y),
                null,
                out localPointStartInCanvas
            );

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRectTransform,
                new Vector2(screenPosEnd.x, screenPosEnd.y),
                null,
                out localPointEndInCanvas
            );

            _hand.anchoredPosition = localPointStartInCanvas;

            var sequence = DOTween.Sequence();

            sequence.Append(_handCanvasGroup.DOFade(1f, fadeDur));

            sequence.Append(_hand.DOAnchorPos(localPointEndInCanvas, moveDur).SetEase(Ease.InOutSine));

            sequence.Append(_handCanvasGroup.DOFade(0f, fadeDur));

            sequence.AppendCallback(() =>
            {
                _hand.anchoredPosition = localPointStartInCanvas;
            });

            sequence.SetLoops(-1, LoopType.Restart);
            sequence.SetTarget(_hand);
        }
    }

    private void PlayShowText(string text)
    {

        _tapText.text = text;
        var topCell = Board.Instance.GetTopCell();
        if (topCell != null)
        {
            _tapTextCanvasGroup.DOFade(1f, 0.5f);

            Vector3 screenPos =
                Camera.main.WorldToScreenPoint(topCell.transform.position + new Vector3(0f, 15.0f, 0f));
            Vector2 localPointInCanvas;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRectTransform,
                new Vector2(screenPos.x, screenPos.y),
                null,
                out localPointInCanvas
            );

            RectTransform rectTransform = _tapTextCanvasGroup.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, localPointInCanvas.y-100f);
        }
    }
}

