namespace BrawlLine.Haptics
{
    using UnityEngine;
    using System;

#if UNITY_IOS
    using System.Collections;
    using System.Runtime.InteropServices;
#endif

    public static class Vibration
    {
#if UNITY_IOS
        [DllImport ( "__Internal" )]
        private static extern bool HasVibratorNative ();

        [DllImport ( "__Internal" )]
        private static extern void VibrateNative ();

        [DllImport ( "__Internal" )]
        private static extern void VibratePopNative ();

        [DllImport ( "__Internal" )]
        private static extern void VibratePeekNative ();

        [DllImport ( "__Internal" )]
        private static extern void VibrateNopeNative ();

        [DllImport("__Internal")]
        private static extern void ImpactOccurredNative(string style);

        [DllImport("__Internal")]
        private static extern void NotificationOccurredNative(string style);

        [DllImport("__Internal")]
        private static extern void SelectionChangedNative();
#endif

#if UNITY_ANDROID
        private static AndroidJavaClass unityPlayer;
        private static AndroidJavaObject currentActivity;
        private static AndroidJavaObject vibrator;
        private static AndroidJavaObject context;
        private static AndroidJavaClass vibrationEffect;
#endif
        private static bool initialized;

        public static void Init()
        {
            if (initialized)
            {
                return;
            }

#if UNITY_ANDROID

            if (Application.isMobilePlatform)
            {
                unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                context = currentActivity.Call<AndroidJavaObject>("getApplicationContext");

                if (AndroidVersion >= 26)
                {
                    vibrationEffect = new AndroidJavaClass("android.os.VibrationEffect");
                }
            }
#endif

            initialized = true;
        }


        public static void VibrateIOS(ImpactFeedbackStyle style)
        {
#if UNITY_IOS
            ImpactOccurredNative(style.ToString());
#endif
        }

        public static void VibrateIOS(NotificationFeedbackStyle style)
        {
#if UNITY_IOS
            NotificationOccurredNative(style.ToString());
#endif
        }

        public static void VibrateIOSSelectionChanged()

        {
#if UNITY_IOS
            SelectionChangedNative();
#endif
        }
        
        public static void VibratePop()
        {
            if (Application.isMobilePlatform)
            {
#if UNITY_IOS
                VibratePopNative ();
#elif UNITY_ANDROID
                VibrateAndroid(50);
#endif
            }
        }
        
        public static void VibratePeek()
        {
            if (Application.isMobilePlatform)
            {
#if UNITY_IOS
                VibratePeekNative ();
#elif UNITY_ANDROID
                VibrateAndroid(100);
#endif
            }
        }
        
        public static void VibrateNope()
        {
            if (Application.isMobilePlatform)
            {
#if UNITY_IOS
                VibrateNopeNative ();
#elif UNITY_ANDROID
                long[] pattern = { 0, 50, 50, 50 };
                VibrateAndroid(pattern, -1);
#endif
            }
        }


#if UNITY_ANDROID

        public static void VibrateAndroid(long milliseconds)
        {
            if (Application.isMobilePlatform)
            {
                if (AndroidVersion >= 26)
                {
                    AndroidJavaObject createOneShot =
                        vibrationEffect.CallStatic<AndroidJavaObject>("createOneShot", milliseconds, -1);
                    vibrator.Call("vibrate", createOneShot);

                }
                else
                {
                    vibrator.Call("vibrate", milliseconds);
                }
            }
        }
        
        public static void VibrateAndroid(long[] pattern, int repeat)
        {
            if (Application.isMobilePlatform)
            {
                if (AndroidVersion >= 26)
                {
                    AndroidJavaObject createWaveform =
                        vibrationEffect.CallStatic<AndroidJavaObject>("createWaveform", pattern, repeat);
                    vibrator.Call("vibrate", createWaveform);

                }
                else
                {
                    vibrator.Call("vibrate", pattern, repeat);
                }
            }
        }
#endif
        
        public static void CancelAndroid()
        {
            if (Application.isMobilePlatform)
            {
#if UNITY_ANDROID
                vibrator.Call("cancel");
#endif
            }
        }

        public static bool HasVibrator()
        {
            if (Application.isMobilePlatform)
            {
#if UNITY_ANDROID
                AndroidJavaClass contextClass = new("android.content.Context");
                string contextVibratorService = contextClass.GetStatic<string>("VIBRATOR_SERVICE");
                AndroidJavaObject systemService =
                    context.Call<AndroidJavaObject>("getSystemService", contextVibratorService);
                
                if (systemService.Call<bool>("hasVibrator"))
                {
                    return true;
                }

                return false;

#elif UNITY_IOS
                return HasVibratorNative ();
#else
                return false;
#endif
            }
            else
            {
                return false;
            }
        }
        
        public static void Vibrate()
        {
#if UNITY_ANDROID || UNITY_IOS

            if (Application.isMobilePlatform)
            {
                Handheld.Vibrate();
            }
#endif
        }

        private static int AndroidVersion
        {
            get
            {
                int versionNumber = 0;
                
                if (Application.platform == RuntimePlatform.Android)
                {
                    string androidVersion = SystemInfo.operatingSystem;
                    int sdkPos = androidVersion.IndexOf("API-", StringComparison.Ordinal);
                    versionNumber = int.Parse(androidVersion.Substring(sdkPos + 4, 2));
                }

                return versionNumber;
            }
        }
    }

    public enum ImpactFeedbackStyle
    {
        Heavy,
        Medium,
        Light,
        Rigid,
        Soft
    }

    public enum NotificationFeedbackStyle
    {
        Error,
        Success,
        Warning
    }
}