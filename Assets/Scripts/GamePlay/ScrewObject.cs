using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class ScrewObject : MonoBehaviour
{
    [SerializeField] private List<GameObject> _screwObjects;

    private bool _isFirstSetup = false;
    
    public void SetHP(int value)
    {
        if (!_isFirstSetup)
        {
            _isFirstSetup = true;
            Debug.Log("SetHP: " + value);
            for (int i = 0; i < _screwObjects.Count; i++)
            {
                _screwObjects[i].SetActive(i < value);
            }
        }
        else
        {
            float distance = 3f;
            float duration = 0.75f;
  
            _screwObjects[value].transform.DOLocalMove(
                _screwObjects[value].transform.localPosition * distance,
                duration
            ).SetEase(Ease.Linear).OnComplete(() =>
            {
                _screwObjects[value].SetActive(false);
            });    
        }
    }
}
