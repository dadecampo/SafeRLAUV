using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace com.zibra.smoke_and_fire.Bridge
{
    internal static class SmokeAndFireBridge
    {

#if UNITY_EDITOR
        // Editor library selection
#if UNITY_EDITOR_WIN
        public const String PluginLibraryName = "ZibraSmokeAndFireNative_Win_Editor";
#elif UNITY_EDITOR_OSX
        public const String PluginLibraryName = "ZibraSmokeAndFireNative_Mac_Editor";
#else
#error Unsupported platform
#endif

#else

// Player library selection
#if UNITY_IOS || UNITY_TVOS
        public const String PluginLibraryName = "__Internal";
#elif UNITY_WSA
        public const String PluginLibraryName = "ZibraSmokeAndFireNative_WSA";
#elif UNITY_STANDALONE_OSX
        public const String PluginLibraryName = "ZibraSmokeAndFireNative_Mac";
#elif UNITY_STANDALONE_WIN
        public const String PluginLibraryName = "ZibraSmokeAndFireNative_Win";
#elif UNITY_ANDROID
        public const String PluginLibraryName = "ZibraSmokeAndFireNative_Android";
#else
#error Unsupported platform
#endif

#endif

#if ZIBRA_EFFECTS_DEBUG
        [StructLayoutAttribute(LayoutKind.Sequential)]
        public struct DebugTimestampItem
        {
            public uint EventType;
            public float ExecutionTime;
        }
#endif

        [DllImport(PluginLibraryName)]
        public static extern IntPtr ZibraSmokeAndFire_GetRenderEventWithDataFunc();

        [DllImport(PluginLibraryName)]
        public static extern IntPtr ZibraSmokeAndFire_GPUReadbackGetData(Int32 InstanceID, UInt32 size);

#if ZIBRA_EFFECTS_DEBUG
        [DllImport(PluginLibraryName)]
        public static extern uint ZibraSmokeAndFire_GetDebugTimestamps(Int32 InstanceID,
                                                                       [In, Out] DebugTimestampItem[] timestampsItems);
#endif

#if UNITY_EDITOR
        [DllImport(PluginLibraryName)]
        private static extern IntPtr ZibraSmokeAndFire_GetHardwareID();

#if !ZIBRA_EFFECTS_NO_LICENSE_CHECK
        [DllImport(PluginLibraryName)]
        public static extern Int32 ZibraSmokeAndFire_GetRandomNumber();

        [DllImport(PluginLibraryName)]
        public static extern void ZibraSmokeAndFire_ValidateLicense(
            [MarshalAs(UnmanagedType.LPStr)] string serverResponse, Int32 responseSize);
#endif
#endif

        [DllImport(PluginLibraryName)]
        public static extern Int32 ZibraSmokeAndFire_GarbageCollect();

        [DllImport(PluginLibraryName)]
        public static extern bool ZibraSmokeAndFire_IsHardwareSupported();

        [DllImport(PluginLibraryName)]
        public static extern IntPtr ZibraSmokeAndFire_GetSimulationPosition(Int32 InstanceID);

        public static Vector3 GetSimulationContainerPosition(Int32 InstanceID)
        {
            IntPtr readbackData = ZibraSmokeAndFire_GetSimulationPosition(InstanceID);
            if (readbackData != IntPtr.Zero)
            {
                float[] vector = new float[3];
                Marshal.Copy(readbackData, vector, 0, 3);
                return new Vector3(vector[0], vector[1], vector[2]);
            }
            else
            {
                return Vector3.zero;
            }
        }

        public enum EventID : int
        {
            None = 0,
            StepPhysics = 1,
            Draw = 2,
            UpdateSolverParameters = 3,
            UpdateManipulatorParameters = 4,
            CreateFluidInstance = 5,
            RegisterSolverBuffers = 6,
            SetRenderParameters = 7,
            RegisterManipulators = 8,
            ReleaseResources = 9,
            InitializeGpuReadback = 10,
            UpdateReadback = 11,
            RegisterRenderResources = 12,
            UpdateSDFObjects = 13
        }

        internal struct EventData
        {
            public int InstanceID;
            public IntPtr ExtraData;
        };

        public enum LogLevel
        {
            Verbose = 0,
            Info = 1,
            Performance = 2,
            Warning = 3,
            Error = 4,
        }

        public enum TextureFormat
        {
            None,
            R8G8B8A8_SNorm,
            R16G16B16A16_SFloat,
            R32G32B32A32_SFloat,
            R16_SFloat,
            R32_SFloat,
        }

        public static TextureFormat ToBridgeTextureFormat(GraphicsFormat format)
        {
            switch (format)
            {
            case GraphicsFormat.R8G8B8A8_UNorm:
                return TextureFormat.R8G8B8A8_SNorm;
            case GraphicsFormat.R16G16B16A16_SFloat:
                return TextureFormat.R16G16B16A16_SFloat;
            case GraphicsFormat.R32G32B32A32_SFloat:
                return TextureFormat.R32G32B32A32_SFloat;
            case GraphicsFormat.R16_SFloat:
                return TextureFormat.R16_SFloat;
            case GraphicsFormat.R32_SFloat:
                return TextureFormat.R32_SFloat;
            default:
                return TextureFormat.None;
            }
        }

#if ZIBRA_EFFECTS_DEBUG
        [StructLayout(LayoutKind.Sequential)]
        public struct DebugMessage
        {
            public IntPtr Text;
            public LogLevel Level;
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct LoggerSettings
        {
            public IntPtr PFNCallback;
            public LogLevel LogLevel;
        };
#endif

#if UNITY_EDITOR
        public static string GetHardwareIDWrapper()
        {
            IntPtr cstr = ZibraSmokeAndFire_GetHardwareID();

            string result = Marshal.PtrToStringAnsi(cstr);
            return result;
        }
#endif

        public static int EventAndInstanceID(EventID eventID, int InstanceID)
        {
            return (int)eventID | (InstanceID << 8);
        }

        public static void SubmitInstanceEvent(CommandBuffer cmd, int instanceID, EventID eventID,
                                               IntPtr data = default)
        {
            EventData eventData;
            eventData.InstanceID = instanceID;
            eventData.ExtraData = data;

            IntPtr eventDataNative = Marshal.AllocHGlobal(Marshal.SizeOf(eventData));
            Marshal.StructureToPtr(eventData, eventDataNative, true);

            cmd.IssuePluginEventAndData(ZibraSmokeAndFire_GetRenderEventWithDataFunc(), (int)eventID, eventDataNative);
        }

        public static bool NeedGarbageCollect()
        {
            switch (UnityEngine.SystemInfo.graphicsDeviceType)
            {
            case GraphicsDeviceType.Vulkan:
            case GraphicsDeviceType.Direct3D12:
            case GraphicsDeviceType.XboxOneD3D12:
#if UNITY_2020_3_OR_NEWER
            case GraphicsDeviceType.GameCoreXboxOne:
            case GraphicsDeviceType.GameCoreXboxSeries:
#endif
                return true;
            default:
                return false;
            }
        }
    }
}
