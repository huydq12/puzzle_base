using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

public class Holder : MonoBehaviour
{
    [SerializeField, DisableIn(PrefabKind.PrefabInstance)] private Renderer _renderer;
    [SerializeField, DisableIn(PrefabKind.PrefabInstance)] private Transform _spawnPos;
    [SerializeField, DisableIn(PrefabKind.PrefabInstance)] private ParticleSystem _conffetiEffect;
    [ReadOnly] public Shooter ShooterOnholder;
    public bool IsOccupied => ShooterOnholder != null;
    public void AssignShooter(Shooter shooter)
    {
        ShooterOnholder = shooter;
        if (shooter == null) return;

        ShooterOnholder.IsMoving = true;
        shooter.Holder = this;

        Vector3 startPos = shooter.transform.position;
        Vector3 endPos = _spawnPos.position;

        float speed = 1.25f;
        float distance = Vector3.Distance(startPos, endPos);
        float duration = distance / speed;

        duration = Mathf.Clamp(duration, 0.2f, 0.6f);

        float jumpPower = Mathf.Lerp(0.25f, 1.25f, distance / 5f);

        Sequence sq = DOTween.Sequence();

        sq.Join(
            shooter.transform
                .DOJump(endPos, jumpPower, 1, duration)
                .SetEase(Ease.OutQuad)
        );

        sq.Join(
            shooter.transform
                .DOScale(1.2f, duration * 0.8f)
        );

        sq.OnComplete(() =>
        {
            shooter.transform.SetParent(_spawnPos);
            shooter.transform.localPosition = Vector3.zero;
            shooter.IsMoving = false;
            shooter.ShowRemaining = true;
            shooter.OnShooterOnHolder();
        });
    }
    public void Clear()
    {
        AssignShooter(null);
        _conffetiEffect.Play();
        AudioManager.Instance.PlaySFX(SFXType.CollectShooter);
    }
}
