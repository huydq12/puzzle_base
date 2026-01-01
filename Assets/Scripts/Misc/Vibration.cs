#if UNITY_IOS
using System.Collections;
using System.Runtime.InteropServices;
#endif
using UnityEngine;
using System.Runtime.InteropServices.ComTypes;

public class Vibration
{
#if UNITY_ANDROID
    private static AndroidJavaClass UnityPlayer;
    private static AndroidJavaClass VibrationEffect;
    private static AndroidJavaObject CurrentActivity;
    private static AndroidJavaObject Vibrator;
    private static AndroidJavaObject Context;
#elif UNITY_IOS
    [DllImport("__Internal")]
    private static extern bool _HasVibrator();
    [DllImport("__Internal")]
    private static extern void _Vibrate();
    [DllImport("__Internal")]
    private static extern void _VibratePop();
    [DllImport("__Internal")]
    private static extern void _VibratePeek();
    [DllImport("__Internal")]
    private static extern void _VibrateNope();
#endif
    private static int AndroidVersion()
    {
        int versionNumber = 0;
        if (Application.platform == RuntimePlatform.Android)
        {
            string androidVersion = SystemInfo.operatingSystem;
            int sdkPos = androidVersion.IndexOf("API-");
            versionNumber = int.Parse(androidVersion.Substring(sdkPos + 4, 2));
        }
        return versionNumber;
    }
    public static bool HasVibrator()
    {
        if (Application.isMobilePlatform)
        {
#if UNITY_ANDROID
            AndroidJavaClass contextClass = new AndroidJavaClass("android.content.Context");
            string Context_VIBRATOR_SERVICE = contextClass.GetStatic<string>("VIBRATOR_SERVICE");
            AndroidJavaObject systemService = Context.Call<AndroidJavaObject>("getSystemService", Context_VIBRATOR_SERVICE);
            return systemService.Call<bool>("hasVibrator");
#elif UNITY_IOS
            return _HasVibrator();
#endif
        }
        return false;
    }
    public static void Initialize()
    {
        if (IsInitialized) return;
        if (Application.isMobilePlatform)
        {
#if UNITY_ANDROID
            UnityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            CurrentActivity = UnityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            Vibrator = CurrentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
            Context = CurrentActivity.Call<AndroidJavaObject>("getApplicationContext");
            if (AndroidVersion() >= 26) VibrationEffect = new AndroidJavaClass("android.os.VibrationEffect");
#endif
        }
        IsInitialized = true;
    }
    public static void Pop()
    {
        if (Application.isMobilePlatform)
        {
#if UNITY_ANDROID
            Vibrate(50);
#elif UNITY_IOS
            _VibratePop();
#endif
        }
    }
    public static void Peek()
    {
        if (Application.isMobilePlatform)
        {
#if UNITY_ANDROID
            Vibrate(100);
#elif UNITY_IOS
            _VibratePeek();
#endif
        }
    }
    public static void Nope()
    {
        if (Application.isMobilePlatform)
        {
#if UNITY_ANDROID
            long[] pattern = { 0, 50, 50, 50 };
            Vibrate(pattern, -1);
#elif UNITY_IOS
            _VibrateNope();
#endif
        }
    }
    public static void Cancel()
    {
        if (Application.isMobilePlatform)
        {
#if UNITY_ANDROID
            Vibrator.Call("cancel");
#endif
        }
    }
    public static void Vibrate(long milliseconds)
    {
        if (Application.isMobilePlatform)
        {
#if UNITY_ANDROID
            if (AndroidVersion() >= 26)
            {
                AndroidJavaObject createOneShot = VibrationEffect.CallStatic<AndroidJavaObject>("createOneShot", milliseconds, -1);
                Vibrator.Call("vibrate", createOneShot);
            }
            else Vibrator.Call("vibrate", milliseconds);
#else
            Handheld.Vibrate();
#endif
        }
    }
    public static void Vibrate(long[] pattern, int repeat)
    {
        if (Application.isMobilePlatform)
        {
#if UNITY_ANDROID
            if (AndroidVersion() >= 26)
            {
                AndroidJavaObject createWaveform = VibrationEffect.CallStatic<AndroidJavaObject>("createWaveform", pattern, repeat);
                Vibrator.Call("vibrate", createWaveform);
            }
            else Vibrator.Call("vibrate", pattern, repeat);
#else
            Handheld.Vibrate();
#endif
        }
    }
    public static bool IsInitialized { get; private set; }
}