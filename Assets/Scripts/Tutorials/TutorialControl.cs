using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class TutorialControl : TutorialBase
{
    [SerializeField] private RectTransform _canvasRectTransform;
    [SerializeField] private Image _handImg;
    [SerializeField] private Image _circleImg;

    public override void Setup()
    {
        base.Setup();
        Type = TutorialType.Control;
        _tutName = Type.ToString();
    }

    public override void GoNextStep()
    {
        base.GoNextStep();
        StartCoroutine(GoNextStepCoroutine());
        IEnumerator GoNextStepCoroutine()
        {

            switch (_currentStep)
            {
                case 1:
                    {
                        _handImg.color = Color.white.With(a: 0);
                        _circleImg.color = Color.white.With(a: 0);

                        yield return new WaitForSeconds(0.2f);
                        PlayHandClick();
                        yield return new WaitForSeconds(0.5f);
                        TutorialManager.Instance.TutorialControlWaitTapLine = true;
                        break;
                    }
                default:
                    {
                        if (IsFinish())
                        {
                            TutorialManager.Instance.TutorialControlWaitTapLine = false;
                            TutorialManager.Instance.TutorialFinish();

                            _circleImg.DOKill();
                            _handImg.DOKill();
                            _handImg.rectTransform.DOKill();
                            _circleImg.rectTransform.DOKill();
                            this.DOKill();
                            
                            Sequence sq = DOTween.Sequence();
                            sq.Join(_handImg.DOFade(0, 0.5f));
                            sq.Join(_circleImg.DOFade(0, 0.5f));

                            yield return sq.WaitForCompletion();

                            Hide();
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

    private void PlayHandClick()
    {
        GridCell tapCell = Board.Instance.CellTaptInTutorialControl();

        if (tapCell != null)
        {
            Vector3 screenPosTapCell = Camera.main.WorldToScreenPoint(tapCell.transform.position + new Vector3(0.2f, 0f, 0.2f));

            Vector2 localPointStartInCanvas;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRectTransform,
                new Vector2(screenPosTapCell.x, screenPosTapCell.y),
                null,
                out localPointStartInCanvas
            );


            _circleImg.rectTransform.anchoredPosition = localPointStartInCanvas;
            _handImg.rectTransform.anchoredPosition = localPointStartInCanvas + new Vector2(-70, 30);
            _handImg.DOFade(1, 0.25f).OnComplete(() =>
            {
                _handImg.rectTransform.localScale = Vector3.one;

                var seq = DOTween.Sequence();
                seq.Append(
                    _handImg.rectTransform
                        .DOScale(0.88f, 0.36f)
                        .SetEase(Ease.OutQuad)
                );
                seq.AppendCallback(() =>
                {
                    _circleImg.rectTransform.localScale = Vector3.zero;
                    _circleImg.color = Color.white;
                    Sequence sq = DOTween.Sequence();
                    sq.Join(_circleImg.DOFade(0, 0.25f).SetEase(Ease.Linear));
                    sq.Join(_circleImg.rectTransform.DOScale(1, 0.25f).SetEase(Ease.Linear));
                });
                seq.Append(
                    _handImg.rectTransform
                        .DOScale(1f, 0.48f)
                );
                seq.AppendInterval(1f);
                seq.SetLoops(-1, LoopType.Restart);
                seq.SetTarget(this);
            });
        }
    }
}
