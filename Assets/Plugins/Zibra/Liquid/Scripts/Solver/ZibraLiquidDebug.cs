using AOT;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using com.zibra.liquid.Bridge;
#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR

namespace com.zibra.liquid.Solver
{
#if ZIBRA_EFFECTS_DEBUG

#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public static class ZibraLiquidDebug
    {
        public enum LogLevel
        {
            Verbose = 0,
            Info = 1,
            Performance = 2,
            Warning = 3,
            Error = 4,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DebugMessage
        {
            public IntPtr Text;
            public ZibraLiquidDebug.LogLevel Level;
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct LoggerSettings
        {
            public IntPtr PFNCallback;
            public ZibraLiquidDebug.LogLevel LogLevel;
        };

        public static string EditorPrefsKey = "ZibraLiquidLogLevel";
        public static LogLevel CurrentLogLevel;

        public static void SetLogLevel(LogLevel level)
        {
            CurrentLogLevel = level;
#if UNITY_EDITOR
            EditorPrefs.SetInt(EditorPrefsKey, (int)level);
#endif // UNITY_EDITOR

            InitializeDebug();
        }
        static ZibraLiquidDebug()
        {
#if UNITY_EDITOR
            CurrentLogLevel = (LogLevel)EditorPrefs.GetInt(EditorPrefsKey, (int)LogLevel.Error);
#else
            CurrentLogLevel = LogLevel.Error;
#endif // UNITY_EDITOR
        }

#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#endif
        static void InitializeDebug()
        {
            DebugLogCallbackT callbackDelegate = new DebugLogCallbackT(DebugLogCallback);
            var settings = new LoggerSettings();
            settings.PFNCallback = Marshal.GetFunctionPointerForDelegate(callbackDelegate);
            settings.LogLevel = CurrentLogLevel;
            IntPtr settingsPtr = Marshal.AllocHGlobal(Marshal.SizeOf(settings));
            Marshal.StructureToPtr(settings, settingsPtr, true);
            // ZibraLiquid_SetDebugLogWrapperPointer(settingsPtr);
            Marshal.FreeHGlobal(settingsPtr);
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void DebugLogCallbackT(IntPtr message);
        [MonoPInvokeCallback(typeof(DebugLogCallbackT))]
        static void DebugLogCallback(IntPtr request)
        {
            DebugMessage message = Marshal.PtrToStructure<DebugMessage>(request);
            string text = Marshal.PtrToStringAnsi(message.Text);
            switch (message.Level)
            {
            case LogLevel.Verbose:
                Debug.Log("ZibraLiquid[verbose]: " + text);
                break;
            case LogLevel.Info:
                Debug.Log("ZibraLiquid: " + text);
                break;
            case LogLevel.Warning:
                Debug.LogWarning(text);
                break;
            case LogLevel.Performance:
                Debug.LogWarning("ZibraLiquid | Performance Warning:" + text);
                break;
            case LogLevel.Error:
                Debug.LogError("ZibraLiquid" + text);
                break;
            default:
                Debug.LogError("ZibraLiquid | Incorrect native log data format.");
                break;
            }
        }

        // [DllImport(LiquidBridge.PluginLibraryName)]
        // static extern void ZibraLiquid_SetDebugLogWrapperPointer(IntPtr callback);
    }
#endif
}