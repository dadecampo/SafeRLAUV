using System.Collections.Generic;
using UnityEngine;
using com.zibra.liquid.Bridge;

namespace com.zibra.liquid.Solver
{
    internal class ZibraLiquidGPUGarbageCollector : MonoBehaviour
    {
        private static bool GarbageCollectorEnabled = false;
        private static List<ComputeBuffer> BuffersToClear = new List<ComputeBuffer>();
        private static List<RenderTexture> TexturesToClear = new List<RenderTexture>();
        private static List<GraphicsBuffer> GraphicsBuffersToClear = new List<GraphicsBuffer>();
        private static List<Texture3D> Textures3DToClear = new List<Texture3D>();
        private static void SafeReleaseImmediate(ComputeBuffer obj)
        {
            if (obj != null)
            {
                obj.Release();
            }
        }

        private static void SafeReleaseImmediate(GraphicsBuffer obj)
        {
            if (obj != null)
            {
                obj.Release();
            }
        }

        private static void SafeReleaseImmediate(RenderTexture obj)
        {
            if (obj != null)
            {
                obj.Release();
            }
        }

        private static void SafeReleaseImmediate(Texture3D obj)
        {
            if (obj != null)
            {
                DestroyImmediate(obj, true);
            }
        }

        public static void SafeRelease(ComputeBuffer obj)
        {
            if (obj == null)
            {
                return;
            }

            if (!LiquidBridge.NeedGarbageCollect())
            {
                SafeReleaseImmediate(obj);
            }
            else
            {
                BuffersToClear.Add(obj);
            }
        }

        public static void SafeRelease(GraphicsBuffer obj)
        {
            if (obj == null)
            {
                return;
            }

            if (!LiquidBridge.NeedGarbageCollect())
            {
                SafeReleaseImmediate(obj);
            }
            else
            {
                GraphicsBuffersToClear.Add(obj);
            }
        }

        public static void SafeRelease(RenderTexture obj)
        {
            if (obj == null)
            {
                return;
            }

            if (!LiquidBridge.NeedGarbageCollect())
            {
                SafeReleaseImmediate(obj);
            }
            else
            {
                TexturesToClear.Add(obj);
            }
        }

        public static void SafeRelease(Texture3D obj)
        {
            if (obj == null)
            {
                return;
            }

            if (!LiquidBridge.NeedGarbageCollect())
            {
                SafeReleaseImmediate(obj);
            }
            else
            {
                Textures3DToClear.Add(obj);
            }
        }

        private static void GCUpdate()
        {
            int isEmpty = LiquidBridge.ZibraLiquid_GarbageCollect();
            if (isEmpty == 1)
            {
                for (int i = 0; i < BuffersToClear.Count; i++)
                {
                    SafeReleaseImmediate(BuffersToClear[i]);
                }
                for (int i = 0; i < TexturesToClear.Count; i++)
                {
                    SafeReleaseImmediate(TexturesToClear[i]);
                }
                for (int i = 0; i < GraphicsBuffersToClear.Count; i++)
                {
                    SafeReleaseImmediate(GraphicsBuffersToClear[i]);
                }
                for (int i = 0; i < Textures3DToClear.Count; i++)
                {
                    SafeReleaseImmediate(Textures3DToClear[i]);
                }
                BuffersToClear.Clear();
                TexturesToClear.Clear();
                GraphicsBuffersToClear.Clear();
                Textures3DToClear.Clear();

                if (GarbageCollectorEnabled)
                {
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.update -= GCUpdate;
#endif
                    GarbageCollectorEnabled = false;
                }
            }
        }

#if !UNITY_EDITOR
        private void Update()
        {
            GCUpdate();
            if (!GarbageCollectorEnabled)
            {
                Destroy(this.gameObject);
            }
        }
#endif

        public static void GCUpdateWrapper()
        {
            if (LiquidBridge.NeedGarbageCollect())
            {
                GCUpdate();
            }
        }

        public static void CreateGarbageCollector()
        {
            if (!LiquidBridge.NeedGarbageCollect())
            {
                return;
            }

            if (GarbageCollectorEnabled)
            {
                // Garbage collector already exists
                // No need to create another one
                return;
            }

#if UNITY_EDITOR
            UnityEditor.EditorApplication.update += GCUpdate;
#else

            var garbageCollector = new GameObject("ZibraLiquid GPU Garbage Collector");
            garbageCollector.AddComponent<ZibraLiquidGPUGarbageCollector>();
            DontDestroyOnLoad(garbageCollector);
#endif
            GarbageCollectorEnabled = true;
        }
    }
}
