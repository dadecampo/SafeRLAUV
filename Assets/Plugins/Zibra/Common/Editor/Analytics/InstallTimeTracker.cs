using com.zibra.common.Editor;
using System;
using UnityEditor;

namespace com.zibra.common.Analytics
{
    internal static class InstallTimeTracker
    {
        private const string INSTALL_TIME_TRACKING_PREF_KEY = "ZibraEffectsInstallTime_";

        [InitializeOnLoadMethod]
        static private void TrackInstall()
        {
            for (int i = 0; i < (int)PluginManager.Effect.Count; i++)
            {
                PluginManager.Effect effect = (PluginManager.Effect)i;
                string effectsName = PluginManager.GetEffectName(effect);
                string installTimeKey = INSTALL_TIME_TRACKING_PREF_KEY + effectsName;

                if (PluginManager.IsAvailable(effect) && !EditorPrefs.HasKey(installTimeKey))
                {
                    EditorPrefs.SetString(installTimeKey, AnalyticsManager.GetCurrentDateString());
                }
            }
        }

        static public string GetInstallTime(PluginManager.Effect effect)
        {
            string effectsName = PluginManager.GetEffectName(effect);
            string installTimeKey = INSTALL_TIME_TRACKING_PREF_KEY + effectsName;
            return EditorPrefs.GetString(installTimeKey);
        }
    }
}
