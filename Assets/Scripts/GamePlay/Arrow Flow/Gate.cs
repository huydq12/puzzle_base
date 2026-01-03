using System.Collections.Generic;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

public class Gate : MonoBehaviour
{
    [SerializeField] private Transform _maskDoor;
    [SerializeField] private TextMeshPro _total;
    [SerializeField] private Transform _belt;
    [SerializeField] private Transform _door;
    [SerializeField] private Transform _currentShooterHolder;
    [SerializeField] private Transform _nextShooterHolder;
    [SerializeField] private Transform _queueShooterHolder;
    [ReadOnly] public List<ShooterData> Shooters;
    private List<Shooter> _shooterInstances = new List<Shooter>();
    public Shooter CurrentShooter { get; private set; }
    private Shooter NextShooter { get; set; }
    private Shooter QueueShooter { get; set; }
    private int _currentShooterIndex = 0;

    public int Total
    {
        get => int.Parse(_total.text);
        set
        {
            _total.text = value.ToString();
        }
    }
    public void Setup(List<ShooterData> datas)
    {
        OpenGate();
        Total = datas.Count;
        Shooters = datas;
        _shooterInstances.Clear();
        _currentShooterIndex = 0;

        for (int i = 0; i < datas.Count; i++)
        {
            Shooter shoot = Instantiate(ShooterController.Instance.ShooterPrefab);
            shoot.transform.SetParent(GetShooterHolderByIndex(i), false);
            shoot.transform.rotation = Quaternion.Euler(0, -90, 0);
            shoot.transform.localScale = (i == 0 ? 0.75f : 0.65f) * Vector3.one;
            shoot.transform.localPosition = Vector3.zero;
            shoot.SetColor(datas[i].Color);
            shoot.Total = datas[i].Counter;
            _shooterInstances.Add(shoot);
        }

        if (_shooterInstances.Count > 0)
            CurrentShooter = _shooterInstances[0];
        if (_shooterInstances.Count > 1)
            NextShooter = _shooterInstances[1];
        if (_shooterInstances.Count > 2)
            QueueShooter = _shooterInstances[2];

        for (int i = 0; i < _shooterInstances.Count; i++)
        {
            _shooterInstances[i].ShowTotal = i == 0;
        }
    }

    [Button]
    public void CloseGate()
    {
        _total.enabled = false;
        _door.gameObject.SetActive(true);
        _maskDoor.gameObject.SetActive(true);
        Sequence sq = DOTween.Sequence();
        sq.Append(_belt.DOScaleX(0.3f, 0.25f));
        sq.Append(_maskDoor.DOLocalMoveY(-1.25f, 0.2f));
    }
    [Button]
    public void OpenGate()
    {
        _total.enabled = true;
        _door.gameObject.SetActive(false);
        _maskDoor.gameObject.SetActive(false);
        _belt.transform.localScale = Vector3.one;
        _maskDoor.localPosition = new Vector3(0, 0.3f, -2.5f);
    }

    private Transform GetShooterHolderByIndex(int index)
    {
        if (index == 0) return _currentShooterHolder;
        if (index == 1) return _nextShooterHolder;
        if (index == 2) return _queueShooterHolder;
        
        return _queueShooterHolder;
    }

    [Button]
    public void CollectCurrentShooter()
    {
        if (_currentShooterIndex > _shooterInstances.Count - 1)
        {
            return;
        }

        var prevCurrent = CurrentShooter;
        var prevNext = NextShooter;
        var prevQueue = QueueShooter;

        if (prevCurrent != null)
            prevCurrent.ShowTotal = false;

        bool isLastShooter = _currentShooterIndex >= _shooterInstances.Count - 1;
        Total = Mathf.Max(0, Shooters.Count - (_currentShooterIndex + 1));
        Sequence seq = DOTween.Sequence();
        if (prevCurrent != null)
        {
            seq.Append(prevCurrent.transform.DOScale(Vector3.zero, 0.25f));
            if (isLastShooter)
            {
                seq.AppendInterval(0.25f);
                seq.Join(prevCurrent.transform.DORotate(new Vector3(0, 180, 0), 0.25f, RotateMode.LocalAxisAdd));
            }
            seq.AppendCallback(() =>
            {
                if (isLastShooter)
                {
                    CloseGate();
                }
                else
                {
                    prevCurrent.transform.SetParent(_queueShooterHolder, false);
                    prevCurrent.transform.localPosition = Vector3.zero;
                    prevCurrent.transform.localScale = 0.75f * Vector3.one;
                    int dataIdx = _currentShooterIndex + 2;
                    if (dataIdx < Shooters.Count)
                    {
                        prevCurrent.SetColor(Shooters[dataIdx].Color);
                        prevCurrent.Total = Shooters[dataIdx].Counter;
                    }
                }
            });
        }

        if (!isLastShooter)
        {
            seq.AppendCallback(() =>
            {
                if (prevNext != null)
                {
                    prevNext.transform.SetParent(_currentShooterHolder);
                    prevNext.transform.DOLocalMove(Vector3.zero, 0.25f);
                    prevNext.transform.DOScale(0.75f * Vector3.one, 0.25f);
                }
                if (prevQueue != null)
                {
                    prevQueue.transform.SetParent(_nextShooterHolder);
                    prevQueue.transform.DOLocalMove(Vector3.zero, 0.25f);
                }
            });
            seq.AppendInterval(0.25f);

            seq.AppendCallback(() =>
            {
                _currentShooterIndex++;
                if (_currentShooterIndex < _shooterInstances.Count)
                    CurrentShooter = _shooterInstances[_currentShooterIndex];
                else
                    CurrentShooter = null;
                if (_currentShooterIndex + 1 < _shooterInstances.Count)
                    NextShooter = _shooterInstances[_currentShooterIndex + 1];
                else
                    NextShooter = null;
                if (_currentShooterIndex + 2 < _shooterInstances.Count)
                    QueueShooter = _shooterInstances[_currentShooterIndex + 2];
                else
                    QueueShooter = null;

                if (CurrentShooter != null)
                    CurrentShooter.ShowTotal = true;
                if (NextShooter != null)
                    NextShooter.ShowTotal = false;
                if (QueueShooter != null)
                    QueueShooter.ShowTotal = false;
            });
        }
    }
}
