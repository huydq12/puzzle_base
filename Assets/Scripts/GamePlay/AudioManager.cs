using UnityEngine;

public enum BGType
{
	Gameplay
}
public enum SFXType
{
	None,
	BlockTapSelect,
	BlockSliding,
	BlockCollisionError,
	TurretShoot,
	BulletHit,
	BlockCollectedHoleIn,
	ComboPitchUp,
	LevelClearConfetti,
	LevelFailed,
	BoosterHammer,
	BoosterWand,
	BoosterDropper,
	UIClick,
	UIClickMenuPause,
}
public class AudioManager : Singleton<AudioManager>
{
	[SerializeField] private AudioConfig _audioConfig;
	[SerializeField] private AudioSource _bgAudioSource;
	[SerializeField] private AudioSource _sfxAudioSource;
	private bool _bgEnabled = true;
	private bool _sfxEnabled = true;
	private new void Awake()
	{
		base.Awake();
		_bgAudioSource.volume = PlayerPrefs.GetFloat("BGVolume", 1);
		_sfxAudioSource.volume = PlayerPrefs.GetFloat("SFXVolume", 1);
		Game.Launch();
		var userData = Game.Data.Load<UserData>();
		if (userData != null)
		{
			SetBGEnabled(userData.musicOn);
			SetSFXEnabled(userData.soundOn);
		}
		PlayBG(BGType.Gameplay);
	}

	public float GetVolumeBG() => _bgAudioSource.volume;
	public float GetVolumeSFX() => _sfxAudioSource.volume;

	public void SetVolumeBG(float volume)
	{
		_bgAudioSource.volume = volume;
		PlayerPrefs.SetFloat("BGVolume", volume);
		PlayerPrefs.Save();
	}
	public void SetVolumeSFX(float volume)
	{
		_sfxAudioSource.volume = volume;
		PlayerPrefs.SetFloat("SFXVolume", volume);
		PlayerPrefs.Save();
	}

	public void SetBGEnabled(bool enabled)
	{
		_bgEnabled = enabled;
		_bgAudioSource.mute = !_bgEnabled;
	}
	public void SetSFXEnabled(bool enabled)
	{
		_sfxEnabled = enabled;
		_sfxAudioSource.mute = !_sfxEnabled;
	}
	public bool IsBGEnabled() => _bgEnabled;
	public bool IsSFXEnabled() => _sfxEnabled;
	public void PlayBG(BGType bgType)
	{
		var clip = _audioConfig.GetBGClipSettings(bgType);
		if (clip != null)
		{
			_bgAudioSource.clip = clip;
			_bgAudioSource.loop = true;
			_bgAudioSource.Play();
		}
	}
	public void PlaySFX(SFXType sfxType)
	{
		var clip = _audioConfig.GetSFXClipSettings(sfxType);
		if (clip != null)
		{
			_sfxAudioSource.PlayOneShot(clip);
		}
	}
}
