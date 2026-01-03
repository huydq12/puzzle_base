using System.Collections;
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
    [ReadOnly] public List<Shooter> Shooters;

    public void Setup(List<ShooterData> datas)
    {
        
    }

    [Button]
    public void CloseGate()
    {
        Sequence sq = DOTween.Sequence();
        sq.Append(_belt.DOScaleX(0.3f, 0.25f));
        sq.Append(_maskDoor.DOLocalMoveY(-1.25f, 0.2f));
    }
    [Button]
    public void OpenGate()
    {
        _belt.transform.localScale = Vector3.one;
        _maskDoor.transform.localPosition = new Vector3(0, 1.25f, 0);
    }
}
