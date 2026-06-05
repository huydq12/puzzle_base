using System.Collections;
using DG.Tweening;
using Spine.Unity;
using UnityEngine;

public class TutorialControl : TutorialBase
{
    [SerializeField] private RectTransform _canvasRectTransform;
    [SerializeField] private RectTransform _titleRectTransform;
    [SerializeField] private SkeletonGraphic hand;
    [SerializeField] private Vector2 _handOffset;
    [SerializeField] private Vector2 _boosterHandOffset = new Vector2(-70f, -70f);

    private Vector3 _titleDefaultScale;
    private bool _isShowingBoosterGuide;

    public override void Setup()
    {
        base.Setup();
        Type = TutorialType.Control;
        _tutName = Type.ToString();

        if (_titleRectTransform != null)
        {
            _titleDefaultScale = _titleRectTransform.localScale;
        }
    }

    public void ShowBoosterGuide(Vector3 worldPosition)
    {
        if (hand == null || _canvasRectTransform == null || Camera.main == null) return;

        gameObject.SetActive(true);
        _isShowingBoosterGuide = true;

        if (_titleRectTransform != null)
        {
            _titleRectTransform.gameObject.SetActive(false);
        }

        hand.DOKill();
        hand.rectTransform.DOKill();
        hand.gameObject.SetActive(true);
        hand.color = Color.white;

        Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPosition);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvasRectTransform,
            new Vector2(screenPos.x, screenPos.y),
            null,
            out Vector2 localPoint);

        hand.rectTransform.anchoredPosition = localPoint + _boosterHandOffset;
        hand.rectTransform.localScale = Vector3.one;

        var seq = DOTween.Sequence();
        seq.Append(hand.rectTransform.DOScale(0.88f, 0.36f).SetEase(Ease.OutQuad));
        seq.Append(hand.rectTransform.DOScale(1f, 0.48f));
        seq.AppendInterval(1f);
        seq.SetLoops(-1, LoopType.Restart);
        seq.SetTarget(this);
    }

    public void HideBoosterGuide()
    {
        if (!_isShowingBoosterGuide) return;

        _isShowingBoosterGuide = false;
        hand?.DOKill();
        if (hand != null)
        {
            hand.rectTransform.DOKill();
            hand.gameObject.SetActive(false);
        }

        if (_titleRectTransform != null)
        {
            _titleRectTransform.gameObject.SetActive(true);
        }

        gameObject.SetActive(false);
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
                        if (hand != null)
                        {
                            hand.color = Color.white.With(a: 0);
                        }

                        if (_titleRectTransform != null)
                        {
                            _titleRectTransform.DOKill();
                            Vector3 s = _titleDefaultScale == default ? _titleRectTransform.localScale : _titleDefaultScale;
                            _titleRectTransform.localScale = new Vector3(0f, s.y, s.z);
                            _titleRectTransform.DOScaleX(s.x, 0.25f).SetEase(Ease.OutBack).SetTarget(this);
                        }

                        yield return new WaitForSeconds(0.2f);
                        PlayHandClick();
                        yield return new WaitForSeconds(0.5f);
                        var tutorialManager = TutorialManager.Instance;
                        if (tutorialManager != null) tutorialManager.TutorialControlWaitTapLine = true;
                        break;
                    }
                default:
                    {
                        if (IsFinish())
                        {
                            var tutorialManager = TutorialManager.Instance;
                            if (tutorialManager != null)
                            {
                                tutorialManager.TutorialControlWaitTapLine = false;
                                tutorialManager.TutorialFinish();
                            }

                            if (hand != null)
                            {
                                hand.DOKill();
                                hand.rectTransform.DOKill();
                            }
                            if (_titleRectTransform != null) _titleRectTransform.DOKill();
                            this.DOKill();
                            
                            Sequence sq = DOTween.Sequence();
                            if (hand != null)
                            {
                                sq.Join(hand.DOFade(0, 0.5f));
                            }

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
        if (hand == null) return;

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


            hand.rectTransform.anchoredPosition = localPointStartInCanvas + _handOffset;
            hand.DOFade(1, 0.25f).OnComplete(() =>
            {
                hand.rectTransform.localScale = Vector3.one;

                var seq = DOTween.Sequence();
                seq.Append(
                    hand.rectTransform
                        .DOScale(0.88f, 0.36f)
                        .SetEase(Ease.OutQuad)
                );
                seq.Append(
                    hand.rectTransform
                        .DOScale(1f, 0.48f)
                );
                seq.AppendInterval(1f);
                seq.SetLoops(-1, LoopType.Restart);
                seq.SetTarget(this);
            });
        }
    }

    private void OnDisable()
    {
        _isShowingBoosterGuide = false;
        hand?.DOKill();
        if (hand != null)
        {
            hand.rectTransform.DOKill();
        }

        if (_titleRectTransform != null)
        {
            _titleRectTransform.gameObject.SetActive(true);
        }
    }
}
