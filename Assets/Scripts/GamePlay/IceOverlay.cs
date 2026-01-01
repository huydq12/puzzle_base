using TMPro;
using UnityEngine;
using System.Collections.Generic;

public class IceOverlay : MonoBehaviour
{
    [SerializeField] private TMP_Text _hpText;
    [SerializeField] private float startPosText = 0.15f;
    [SerializeField] private float spacing = 0.2f;
    [SerializeField] private IceObject _iceOverlay;
    [SerializeField] private List<IceObject> _iceOverlayPrefab;
    private int _hp;
    private int _stack;

    private bool isSpawn = false;

    public void SetHP(int value)
    {
        _hp = value;
        UpdateText();
    }

    public void SetStack(int value)
    {
        _stack = value;
        UpdateText();
    }

    public void SetInfo(int hp, int stack,float localY)
    {
        _hp = hp;
        _stack = stack;
        UpdateText();
        _hpText.transform.localPosition = new Vector3(_hpText.transform.localPosition.x, startPosText+localY, _hpText.transform.localPosition.z);

        if (!isSpawn)
        {
            _iceOverlay.SetActive(true);
            Debug.Log("Spawn");
            for (int i = 0; i < _stack; i++)
            {
                var overlay = Instantiate(_iceOverlay, transform);
                overlay.transform.localPosition = new Vector3(0,(i+1) * spacing,0);
                _iceOverlayPrefab.Add(overlay);
            }

            isSpawn = true;
        }
        else
        {
            for (int i = 0; i < _iceOverlayPrefab.Count; i++)
            {
               _iceOverlayPrefab[i].SetHP(_hp);
            }
        }

    }

    private void UpdateText()
    {
        if (_hpText != null)
        {
            _hpText.text = $"{_hp*10}";
        }
    }
}
