using UnityEngine;

public class ScrewOverlay : MonoBehaviour
{
    [SerializeField] private float spacing = 0.2f;
    [SerializeField] private ScrewObject _screwOverlay;
    private ScrewObject _screwOverlayPrefab;
    private int _hp;
    private int _stack;
    private float _stackLocalY;
    private int _lastSentHp = int.MinValue;

    private bool isSpawn = false;

    public void SetHP(int value)
    {
        _hp = value;
        UpdateView();
    }

    public void SetStack(int value)
    {
        _stack = value;
        UpdateView();
    }

    public void SetInfo(int hp, int stack, float localY)
    {
        _hp = hp;
        _stack = stack;
        _stackLocalY = localY;

        if (!isSpawn)
        {
            _screwOverlayPrefab = Instantiate(_screwOverlay, transform);
            isSpawn = true;
        }

        UpdateView();

    }

    private void UpdateView()
    {
        if (!isSpawn || _screwOverlayPrefab == null) return;
        var t = _screwOverlayPrefab.transform;
        t.localPosition = new Vector3(0f, 0f, 0f);

        if (_lastSentHp != _hp)
        {
            _lastSentHp = _hp;
            _screwOverlayPrefab.SetHP(_hp);
        }
    }
}
