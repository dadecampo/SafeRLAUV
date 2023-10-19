using AOT;
using System;
using System.Runtime.InteropServices;
#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR
using UnityEngine;
using com.zibra.smoke_and_fire.Bridge;

namespace com.zibra.smoke_and_fire.Solver
{
#if ZIBRA_EFFECTS_DEBUG

#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public static class ZibraSmokeAndFireDebug
    {
        public static string EditorPrefsKey = "ZibraSmokeAndFiresLogLevel";
        internal static SmokeAndFireBridge.LogLevel CurrentLogLevel;

        internal static void SetLogLevel(SmokeAndFireBridge.LogLevel level)
        {
            CurrentLogLevel = level;
#if UNITY_EDITOR
            EditorPrefs.SetInt(EditorPrefsKey, (int)level);
#endif // UNITY_EDITOR
            InitializeDebug();
        }

        static ZibraSmokeAndFireDebug()
        {
#if UNITY_EDITOR
            CurrentLogLevel =
                (SmokeAndFireBridge.LogLevel)EditorPrefs.GetInt(EditorPrefsKey, (int)SmokeAndFireBridge.LogLevel.Error);
#else
            CurrentLogLevel = SmokeAndFireBridge.LogLevel.Error;
#endif // UNITY_EDITOR
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#endif
        static void InitializeDebug()
        {
            DebugLogCallbackT callbackDelegate = new DebugLogCallbackT(DebugLogCallback);
            var settings = new SmokeAndFireBridge.LoggerSettings();
            settings.PFNCallback = Marshal.GetFunctionPointerForDelegate(callbackDelegate);
            settings.LogLevel = CurrentLogLevel;
            IntPtr settingsPtr = Marshal.AllocHGlobal(Marshal.SizeOf(settings));
            Marshal.StructureToPtr(settings, settingsPtr, true);
            ZibraSmokeAndFire_SetDebugLogWrapperPointer(settingsPtr);
            Marshal.FreeHGlobal(settingsPtr);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void DebugLogCallbackT(IntPtr message);
        [MonoPInvokeCallback(typeof(DebugLogCallbackT))]
        static void DebugLogCallback(IntPtr request)
        {
            SmokeAndFireBridge.DebugMessage message = Marshal.PtrToStructure<SmokeAndFireBridge.DebugMessage>(request);
            string text = Marshal.PtrToStringAnsi(message.Text);
            switch (message.Level)
            {
            case SmokeAndFireBridge.LogLevel.Verbose:
                Debug.Log("ZibraSmokeAndFire[silly]: " + text);
                break;
            case SmokeAndFireBridge.LogLevel.Info:
                Debug.Log("ZibraSmokeAndFire: " + text);
                break;
            case SmokeAndFireBridge.LogLevel.Warning:
                Debug.LogWarning(text);
                break;
            case SmokeAndFireBridge.LogLevel.Performance:
                Debug.LogWarning("ZibraSmokeAndFire | Performance Warning:" + text);
                break;
            case SmokeAndFireBridge.LogLevel.Error:
                Debug.LogError("ZibraSmokeAndFire" + text);
                break;
            default:
                Debug.LogError("ZibraSmokeAndFire | Incorrect native log data format.");
                break;
            }
        }

        [DllImport(SmokeAndFireBridge.PluginLibraryName)]
        static extern void ZibraSmokeAndFire_SetDebugLogWrapperPointer(IntPtr callback);
    }
#endif
}