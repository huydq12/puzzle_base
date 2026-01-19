using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private TrailRenderer _trail;

    public bool ShowTrail
    {
        get => _trail.enabled;
        set => _trail.enabled = value;
    }
}
