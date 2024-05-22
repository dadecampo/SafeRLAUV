using com.zibra.common.Editor;
using System.Collections.Generic;
using UnityEditor;
using static com.zibra.common.Editor.PluginManager;

namespace com.zibra.common.Analytics
{
    internal static class InstallTracking
    {
        private const string INSTALL_TRACKING_PREF_KEY = "ZibraEffectsInstallTracking";

        [InitializeOnLoadMethod]
        static private void TrackInstall()
        {
#if ZIBRA_EFFECTS_OTP_VERSION
            string liquidName = PluginManager.GetEffectName(Effect.Liquid);
            if (PluginManager.IsAvailable(Effect.Liquid) && !EditorPrefs.HasKey(INSTALL_TRACKING_PREF_KEY + liquidName))
            {
                EditorPrefs.SetBool(INSTALL_TRACKING_PREF_KEY + liquidName, true);

                AnalyticsManager.AnalyticsEvent liquidEvent = new AnalyticsManager.AnalyticsEvent
                {
                    EventType = "Liquid_new_user",
                    Properties = new Dictionary<string, object>
                    {
                    }
                };
                AnalyticsManager.GetInstance(Effect.Liquid).TrackEvent(liquidEvent);
                if (PluginManager.IsAvailable(Effect.Smoke))
                {
                    AnalyticsManager.GetInstance(Effect.Smoke).TrackEvent(liquidEvent);
                }
            }

            string smokeName = PluginManager.GetEffectName(Effect.Smoke);
            if (PluginManager.IsAvailable(Effect.Smoke) && !EditorPrefs.HasKey(INSTALL_TRACKING_PREF_KEY + smokeName))
            {
                EditorPrefs.SetBool(INSTALL_TRACKING_PREF_KEY + smokeName, true);

                AnalyticsManager.AnalyticsEvent smokeEvent = new AnalyticsManager.AnalyticsEvent
                {
                    EventType = "SF_new_user",
                    Properties = new Dictionary<string, object>
                    {
                    }
                };
                AnalyticsManager.GetInstance(Effect.Smoke).TrackEvent(smokeEvent);
                if (PluginManager.IsAvailable(Effect.Liquid))
                {
                    AnalyticsManager.GetInstance(Effect.Liquid).TrackEvent(smokeEvent);
                }
            }
#else
            string effectsName = "effects";
            if (!EditorPrefs.HasKey(INSTALL_TRACKING_PREF_KEY + effectsName))
            {
                EditorPrefs.SetBool(INSTALL_TRACKING_PREF_KEY + effectsName, true);

                AnalyticsManager.GetInstance(effectsName).TrackEvent(new AnalyticsManager.AnalyticsEvent
                {
                    EventType = "Effects_new_user",
                    Properties = new Dictionary<string, object>
                    {
                        { "Unity_Pro", UnityEditorInternal.InternalEditorUtility.HasPro() },
                    }
                });
            }
#endif
        }
    }
}