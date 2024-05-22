using com.zibra.common.Editor;
using static com.zibra.common.Editor.PluginManager;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.PlayerLoop;
using com.zibra.common.Editor.Licensing;

namespace com.zibra.common.Analytics
{
    internal class ActivationTracking
    {
#region Public Interface
        public static ActivationTracking GetInstance(Effect effect)
        {
            return GetInstance(PluginManager.GetEffectName(effect));
        }

        public static ActivationTracking GetInstance(string effect)
        {
#if ZIBRA_EFFECTS_OTP_VERSION
            return Instances[effect];
#else
            return Instance;
#endif
        }

        public void StoreActivationTrigger(string trigger)
        {
            ActivationTrigger = trigger;
        }

        public void SetAutomaticActivation(bool isAutomatic)
        {
            IsAutomaticActivation = isAutomatic;
        }

        public void TrackActivation()
        {
            if (IsAutomaticActivation || IsTracked)
            {
                return;
            }

            IsTracked = true;

            Dictionary<string, object> properties = new Dictionary<string, object>
            {
                { "LicenseKey", LicensingManager.Instance.GetSavedLicenseKey(EffectType) },
                { "ActivationTrigger", ActivationTrigger }
            };
#if ZIBRA_EFFECTS_OTP_VERSION
            string eventName = ((EffectType == Effect.Liquid) ? "Liquid_user_activated" : "SF_user_activated");
            AnalyticsManager.GetInstance(EffectType).TrackEvent(new AnalyticsManager.AnalyticsEvent
#else
            string eventName = "Effects_user_activated";
            AnalyticsManager.GetInstance("effects").TrackEvent(new AnalyticsManager.AnalyticsEvent
#endif
            {
                EventType = eventName,
                Properties = properties
            });
        }

#endregion
#region Implementation details
        static ActivationTracking()
        {
#if ZIBRA_EFFECTS_OTP_VERSION
            string liquid = GetEffectName(Effect.Liquid);
            Instances.Add(liquid, new ActivationTracking(Effect.Liquid));
            string smoke = GetEffectName(Effect.Smoke);
            Instances.Add(smoke, new ActivationTracking(Effect.Smoke));
#else
            Instance = new ActivationTracking(Effect.Smoke);
#endif
        }

        private ActivationTracking(Effect effect)
        {
            EffectType = effect;
        }

#if ZIBRA_EFFECTS_OTP_VERSION
        private static Dictionary<string, ActivationTracking> Instances = new Dictionary<string, ActivationTracking>();
#else
        private static ActivationTracking Instance;
#endif
        private Effect EffectType;
        private string ActivationTrigger = "other";
        private bool IsAutomaticActivation = false;
        private bool IsTracked = false;

#endregion
    }
}