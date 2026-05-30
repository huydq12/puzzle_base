using DG.Tweening;
using UnityEngine;


    public class ConfigSettings : MonoBehaviour
    {
        void Awake()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR

        QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = Mathf.Max(60, (int)Screen.currentResolution.refreshRateRatio.value); 
#endif
            DOTween.Init();
            DOTween.defaultEaseType = Ease.Linear;
            DOTween.SetTweensCapacity(1000, 500);
        }
    }
