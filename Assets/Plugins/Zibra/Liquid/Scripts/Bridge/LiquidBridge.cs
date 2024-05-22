using System;
using System.Runtime.InteropServices;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace com.zibra.liquid.Bridge
{
    internal static class LiquidBridge
    {
#if UNITY_EDITOR

// Editor library selection
#if UNITY_EDITOR_WIN
        public const String PluginLibraryName = "ZibraLiquidNative_Win_Editor";
#elif UNITY_EDITOR_OSX
        public const String PluginLibraryName = "ZibraLiquidNative_Mac_Editor";
#else
#error Unsupported platform
#endif

#else

// Player library selection
#if UNITY_IOS || UNITY_TVOS
        public const String PluginLibraryName = "__Internal";
#elif UNITY_WSA
        public const String PluginLibraryName = "ZibraLiquidNative_WSA";
#elif UNITY_STANDALONE_OSX
        public const String PluginLibraryName = "ZibraLiquidNative_Mac";
#elif UNITY_STANDALONE_WIN
        public const String PluginLibraryName = "ZibraLiquidNative_Win";
#elif UNITY_ANDROID
        public const String PluginLibraryName = "ZibraLiquidNative_Android";
#else
#error Unsupported platform
#endif

#endif

#if ZIBRA_EFFECTS_PROFILING_ENABLED
        [StructLayout(LayoutKind.Sequential)]
        public struct DebugTimestampItem
        {
            public uint EventType;
            public float ExecutionTime;
        }
#endif

        [DllImport(PluginLibraryName)]
        public static extern IntPtr ZibraLiquid_GetRenderEventWithDataFunc();

        [DllImport(PluginLibraryName)]
        public static extern IntPtr ZibraLiquid_GPUReadbackGetData(Int32 InstanceID, UInt32 size);

        [DllImport(PluginLibraryName)]
        public static extern Int32 ZibraLiquid_IsLoaded();

        [DllImport(PluginLibraryName)]
        public static extern void ZibraLiquid_WaitLoad();

        [DllImport(PluginLibraryName)]
        public static extern Int32 ZibraLiquid_GarbageCollect();

#if ZIBRA_EFFECTS_PROFILING_ENABLED
        [DllImport(PluginLibraryName, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint ZibraLiquid_GetDebugTimestamps(Int32 InstanceID,
                                                                 [In, Out] DebugTimestampItem[] timestampsItems);
#endif

#if UNITY_EDITOR
        [DllImport(PluginLibraryName)]
        public static extern Int32 ZibraLiquid_GetCurrentAffineBufferIndex(Int32 InstanceID);

        [DllImport(PluginLibraryName)]
        private static extern IntPtr ZibraLiquid_GetHardwareID();

#if !ZIBRA_EFFECTS_NO_LICENSE_CHECK
        [DllImport(PluginLibraryName)]
        public static extern Int32 ZibraLiquid_GetRandomNumber();

        [DllImport(PluginLibraryName)]
        public static extern void ZibraLiquid_ValidateLicense([MarshalAs(UnmanagedType.LPStr)] string serverResponse,
                                                              Int32 responseSize);
#endif
#endif

        public enum EventID : int
        {
            None = 0,
            StepPhysics = 1,
            DrawLiquid = 2,
            UpdateLiquidParameters = 3,
            UpdateManipulatorParameters = 4,
            ClearSDFAndID = 5,
            CreateFluidInstance = 6,
            RegisterParticlesBuffers = 7,
            SetCameraParameters = 8,
            SetRenderParameters = 9,
            RegisterManipulators = 10,
            RegisterSolverBuffers = 11,
            RegisterRenderResources = 12,
            ReleaseResources = 13,
            InitializeGpuReadback = 14,
            UpdateReadback = 15,
            SetCameraParams = 16,
            UpdateMeshRenderGlobalParameters = 17,
            InitializeGraphicsPipeline = 18,
            UpdateSolverParameters = 19,
            UpdateSDFObjects = 20,
            RenderSDF = 21,
            DrawEffectParticles = 22
        }

        internal struct EventData
        {
            public int InstanceID;
            public IntPtr ExtraData;
        };

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

#if UNITY_EDITOR
        public static string GetHardwareIDWrapper()
        {
            IntPtr cstr = ZibraLiquid_GetHardwareID();
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

            cmd.IssuePluginEventAndData(ZibraLiquid_GetRenderEventWithDataFunc(), (int)eventID, eventDataNative);
        }

        public static bool NeedGarbageCollect()
        {
            switch (UnityEngine.SystemInfo.graphicsDeviceType)
            {
            case GraphicsDeviceType.Vulkan:
            case GraphicsDeviceType.Direct3D12:
            case GraphicsDeviceType.XboxOneD3D12:
            case GraphicsDeviceType.GameCoreXboxOne:
            case GraphicsDeviceType.GameCoreXboxSeries:
                return true;
            default:
                return false;
            }
        }
    }
}
