using com.zibra.common.Editor;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using static com.zibra.common.Editor.PluginManager;
using com.zibra.common.Editor.Licensing;

namespace com.zibra.common.Analytics
{
    [InitializeOnLoad]
    internal class AnalyticsManager
    {
#region Internal Interface
        /// @cond SHOW_INTERNAL
        public static AnalyticsManager GetInstance(Effect effect)
        {
            return GetInstance(PluginManager.GetEffectName(effect));
        }

        public static AnalyticsManager GetInstance(string effect)
        {
#if ZIBRA_EFFECTS_OTP_VERSION
            return Instances[effect];
#else
            return Instance;
#endif
        }

        public struct AnalyticsEvent
        {
            public string EventType;
            public Dictionary<string, object> Properties;
        }

        public void TrackEvent(AnalyticsEvent e)
        {
            // Don't track events in batch mode
            if (Application.isBatchMode)
            {
                return;
            }

            if (SendDelay == 0)
            {
                EditorApplication.update += Update;
            }

            SendDelay = SEND_DELAY;

            e.Properties.TryAdd("Date", GetCurrentDateString());
            SaveEvent(e);
        }
#endregion
#region Implementation details
        static AnalyticsManager()
        {
#if ZIBRA_EFFECTS_OTP_VERSION
            string liquid = GetEffectName(Effect.Liquid);
            Instances.Add(liquid, new AnalyticsManager(liquid));
            string smoke = GetEffectName(Effect.Smoke);
            Instances.Add(smoke, new AnalyticsManager(smoke));
#else
            string effects = "effects";
            Instance = new AnalyticsManager(effects);
#endif
        }

        private AnalyticsManager(string effect)
        {
            EffectName = effect;

            // Don't process any events in batch mode
            if (Application.isBatchMode)
            {
                return;
            }

            RestoreEvents();
        }

        struct CommonDataStruct
        {
            public string Product;
            public string HardwareId;
            public string LicenseKey;
            public bool Liquid_installed;
            public bool SF_installed;
            public string PluginVersionNumber;
            public string DeveloperOS;
            public string EditorsGraphicsAPI;
            public string EngineVersion;
            public string InstallDate;
            public bool IsDeferred;
        }

        struct RequestData
        {
            public CommonDataStruct CommonData;
            public List<AnalyticsEvent> Events;
            public string APIVersion;
        }

        private const string ANALYTICS_URL = "https://analytics.zibra.ai/api/usageAnalytics";
        private const string API_VERSION = "2024.03.06";
        private const int SUCCESS_CODE = 201;
        private const int SEND_DELAY = 2;
        private const int MAX_SAVED_EVENTS = 500;
        private const int MAX_SAVED_DAYS = 30;
        private const string SAVED_EVENTS_PREFS_KEY = "ZibraEffectsAnalyticsSavedEvents";

        private string EffectName;
        private int SendDelay = 0;
        private List<AnalyticsEvent> EventsToSend = new List<AnalyticsEvent>();
        private bool IsDeferred = false;

#if ZIBRA_EFFECTS_OTP_VERSION
        private static Dictionary<string, AnalyticsManager> Instances = new Dictionary<string, AnalyticsManager>();
#else
        private static AnalyticsManager Instance;
#endif

        public static string GetCurrentDateString()
        {
            return DateToISO8601String(DateTime.UtcNow);
        }

        public static string DateToISO8601String(DateTime date)
        {
            return date.ToString("yyyy-MM-ddTHH:mm:ssZ");
        }

        CommonDataStruct GetCommonData()
        {
            CommonDataStruct result = new CommonDataStruct();

            Effect effectType = ParseEffectName(EffectName); 
            if (effectType == Effect.Count)
            {
                effectType = Effect.Liquid;
            }

            result.Product = EffectName;
            result.HardwareId = PluginManager.GetHardwareID();
            result.LicenseKey = LicensingManager.Instance.GetSavedLicenseKey(effectType);
            result.Liquid_installed = PluginManager.IsAvailable(Effect.Liquid);
            result.SF_installed = PluginManager.IsAvailable(Effect.Smoke);
            result.PluginVersionNumber = Effects.Version;
            result.DeveloperOS = SystemInfo.operatingSystemFamily.ToString();
            result.EditorsGraphicsAPI = SystemInfo.graphicsDeviceType.ToString();
            result.EngineVersion = Application.unityVersion.ToString();
            result.InstallDate = InstallTimeTracker.GetInstallTime(effectType);
            result.IsDeferred = IsDeferred;

            return result;
        }

        private void Update()
        {
            --SendDelay;

            if (SendDelay > 0)
                return;

            EditorApplication.update -= Update;

            SendEvents(EventsToSend);
            ClearEvents();
        }

        private void SaveEvent(AnalyticsEvent e)
        {
            EventsToSend.Add(e);
            SerializeEvents();
        }

        private void SaveEvents(List<AnalyticsEvent> events)
        {
            EventsToSend.AddRange(events);
            SerializeEvents();
        }

        private void FilterEvents()
        {
            EventsToSend.Sort((e1, e2) =>
            {
                string date1 = e1.Properties["Date"] as string;
                string date2 = e2.Properties["Date"] as string;
                return date1.CompareTo(date2);
            }
            );
            if (EventsToSend.Count > MAX_SAVED_EVENTS)
            {
                EventsToSend.RemoveRange(0, EventsToSend.Count - MAX_SAVED_EVENTS);
            }

            string cutoffDate = DateToISO8601String(DateTime.UtcNow.AddDays(-MAX_SAVED_DAYS));
            while (EventsToSend.Count > 0)
            {
                string date = EventsToSend[0].Properties["Date"] as string;
                if (date.CompareTo(cutoffDate) < 0)
                {
                    EventsToSend.RemoveAt(0);
                }
                else
                {
                    break;
                }
            }
        }

        private void SerializeEvents()
        {
            FilterEvents();

            string json = JsonConvert.SerializeObject(EventsToSend);
            EditorPrefs.SetString(SAVED_EVENTS_PREFS_KEY + EffectName, json);
        }

        private void ClearEvents()
        {
            EventsToSend = new List<AnalyticsEvent>();
            EditorPrefs.DeleteKey(SAVED_EVENTS_PREFS_KEY + EffectName);
            IsDeferred = false;
        }

        private void RestoreEvents()
        {
            string json = EditorPrefs.GetString(SAVED_EVENTS_PREFS_KEY + EffectName, null);
            if (string.IsNullOrEmpty(json))
                return;

            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.DateParseHandling = DateParseHandling.None;

            EventsToSend = JsonConvert.DeserializeObject<List<AnalyticsEvent>>(json, settings);

            if (EventsToSend.Count > 0)
            {
                if (SendDelay == 0)
                {
                    EditorApplication.update += Update;
                }

                IsDeferred = true;
                SendDelay = SEND_DELAY;
            }
        }

        private void SendEvents(List<AnalyticsEvent> events)
        {
            RequestData requestData = new RequestData();
            requestData.CommonData = GetCommonData();
            requestData.Events = events;
            requestData.APIVersion = API_VERSION;
            string json = JsonConvert.SerializeObject(requestData);
#if UNITY_2022_2_OR_NEWER
            UnityWebRequest request = UnityWebRequest.PostWwwForm(ANALYTICS_URL, json);
#else
            UnityWebRequest request = UnityWebRequest.Post(ANALYTICS_URL, json);
#endif
            UnityWebRequestAsyncOperation requestOperation = request.SendWebRequest();
            requestOperation.completed += (operation) =>
            {
                UnityWebRequestAsyncOperation requestOperation = operation as UnityWebRequestAsyncOperation;
                UnityWebRequest request = requestOperation.webRequest;
                RequestCompleted(request, events);
            };
        }

        private void RequestCompleted(UnityWebRequest request, List<AnalyticsEvent> events)
        {
            if (request.result != UnityWebRequest.Result.Success ||
                request.responseCode != SUCCESS_CODE)
            {
                // On unsuccessful request, save events to send them later
                SaveEvents(events);
                IsDeferred = true;
            }
            request.Dispose();
        }
        /// @endcond
#endregion
    }
}
