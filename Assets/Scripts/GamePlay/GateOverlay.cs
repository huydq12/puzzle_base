using UnityEngine;
using TMPro;

public class GateOverlay : MonoBehaviour
{
    [SerializeField] private TMP_Text _countText;
    [SerializeField] private float startPosText = 0.15f;
    [SerializeField] private float spacing = 0.2f;
    public GameObject _gateOverlayPrefab;
    [SerializeField] private float _gateOverlayScale = 0.3f;
    public void SetStackCount(int count)
    {
        if (_countText == null)
        {
            return;
        }

        if (count <= 0)
        {
            _countText.gameObject.SetActive(false);
            _countText.text = string.Empty;
            return;
        }

        _countText.gameObject.SetActive(true);
        _countText.text = count.ToString();
        _countText.transform.localPosition = new Vector3(_countText.transform.localPosition.x, startPosText+(count*spacing), _countText.transform.localPosition.z);
        _gateOverlayPrefab.transform.localScale = new Vector3(1, (count*spacing) + _gateOverlayScale, 1);
    }

    public void SetDirection(HexEdge dir)
    {
        float y = dir switch
        {
            HexEdge.Top => 0f,
            HexEdge.TopRight => 60f,
            HexEdge.BottomRight => 120f,
            HexEdge.Bottom => 180f,
            HexEdge.BottomLeft => 240f,
            HexEdge.TopLeft => 300f,
            _ => 0f
        };

        transform.localRotation = Quaternion.Euler(0f, y, 0f);

        if (_countText != null)
        {
            _countText.transform.localRotation = Quaternion.Euler(90f, -y, 0f);
        }
    }
}
