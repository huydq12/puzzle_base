using TMPro;
using UnityEngine;

public class LockOverlay : MonoBehaviour
{
    [SerializeField] private TMP_Text _countText;

    public void SetCount(int value)
    {
        if (_countText != null)
        {
            _countText.text = (value/10).ToString();
        }
    }
}
