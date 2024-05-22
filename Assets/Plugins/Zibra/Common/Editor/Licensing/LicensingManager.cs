using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using com.zibra.common.Editor.SDFObjects;
using com.zibra.common.Utilities;
using static com.zibra.common.Editor.PluginManager;
using com.zibra.common.Analytics;

namespace com.zibra.common.Editor.Licensing
{
    /// <summary>
    ///     Class responsible for managing licensing and allowing server communication.
    /// </summary>
    [InitializeOnLoad]
    public class LicensingManager
    {
#region Public Interface

        /// <summary>
        ///     Status of license validation.
        /// </summary>
        public enum Status
        {
            NotInitialized,
            OK,
            ValidationInProgress,
            NetworkError,
            ValidationError,
#if !ZIBRA_EFFECTS_OTP_VERSION
            NoMaintance,
            Expired,
#endif
            NoKey,
            NotInstalled,
        }

        /// <summary>
        ///     Instance of licensing manager.
        /// </summary>
        public static LicensingManager Instance { get
            {
                if (_Instance == null)
                {
                    _Instance = new LicensingManager();
                }
                return _Instance;
            }
        }

        /// <summary>
        ///     Checks whether specified string is correctly formated key.
        /// </summary>
        public static bool ValidateKeyFormat(string key)
        {
            return Regex.IsMatch(key, KEY_FORMAT_REGEX);
        }

        /// <summary>
        ///     Saves license key intended for single effect and starts validation.
        /// </summary>
        public void ValidateLicense(string key, Effect effect)
        {
            ValidateLicense(key, new Effect[] { effect });
        }

        /// <summary>
        ///     Saves license key intended for list of effects and starts validation.
        /// </summary>
        public void ValidateLicense(string key, Effect[] effects)
        {
            if (!ValidateKeyFormat(key))
            {
                throw new ArgumentException("License key has invalid format.");
            }

            foreach (Effect effect in effects)
            {
                if (effect == Effect.Count)
                {
                    throw new ArgumentException("Count is not valid value for effect");
                }
                if (IsLicenseVerified(effect))
                {
                    throw new ArgumentException("Effect " + effect + " already has verified license.");
                }
            }

            foreach (Effect effect in effects)
            {
                Statuses[(int)effect] = Status.NotInitialized;
                LicenseKeys[(int)effect] = key;
            }

            SaveLicenseKeys();
            ValidateLicense();
        }

        /// <summary>
        ///     Validates saved license keys.
        /// </summary>
        public void ValidateLicense()
        {
            ValidateEffectStatuses();

            List<Effect> toValidate = new List<Effect>();

            for (int i = 0; i < (int)Effect.Count; ++i)
            {
                Effect effect = (Effect)i;
                switch (Statuses[i])
                {
                    case Status.NotInitialized:
                    case Status.NetworkError:
                        toValidate.Add(effect);
                        break;
                }
            }

            if (toValidate.Count == 0)
            {
                return;
            }

            SendValidationRequest(toValidate);
            SetStatus(toValidate, Status.ValidationInProgress);
        }

        /// <summary>
        ///     Removes all saved license keys and resets license validation status.
        /// </summary>
        public void RemoveLicenses()
        {
            LicenseKeys = new string[(int)Effect.Count];
            Statuses = new Status[(int)Effect.Count];
            ServerErrors = new string[(int)Effect.Count];
            SaveLicenseKeys();
            SaveSessionState();
#if ZIBRA_EFFECTS_OTP_VERSION
            RegistrationManager.Reset();
#endif
            for (int i = 0; i < (int)Effect.Count; ++i)
            {
                Effect effect = (Effect)i;
                ActivationTracking.GetInstance(effect).SetAutomaticActivation(false);
            }
            LicenseInfoQuery.Instance.Reset();
            OnLicenseStatusUpdate?.Invoke();
        }

        /// <summary>
        ///     Returns list of saved license keys.
        /// </summary>
        public string[] GetSavedLicenseKeys()
        {
            List<string> filteredKeys = new List<string>();
            foreach (string key in LicenseKeys)
            {
                if (!string.IsNullOrEmpty(key) && !filteredKeys.Contains(key))
                {
                    filteredKeys.Add(key);
                }
            }
            return filteredKeys.ToArray();
        }

        /// <summary>
        ///     Returns saved license key for specified effect.
        /// </summary>
        public string GetSavedLicenseKey(Effect effect)
        {
            return LicenseKeys[(int)effect];
        }

        /// <summary>
        ///     Returns license validation status for specified effect.
        /// </summary>
        public Status GetStatus(Effect effect)
        {
            return Statuses[(int)effect];
        }

        /// <summary>
        ///     Returns human readable error describing error of license validation for specified effect.
        /// </summary>
        public string GetErrorMessage(Effect effect)
        {
            Status status = Statuses[(int)effect];
            switch (status)
            {
                case Status.ValidationInProgress:
                    return "License key validation in progress. Please wait.";
                case Status.NetworkError:
                    return "Network error. Please ensure you are connected to the Internet and try again.";
                case Status.ValidationError:
                    return ServerErrors[(int)effect];
                case Status.NoKey:
                    return "Plugin is not registered.";
#if !ZIBRA_EFFECTS_OTP_VERSION
                case Status.NoMaintance:
                    return "License expired.";
#endif
                case Status.NotInstalled:
                    return "Specified effect is not installed.";
                default:
                    return "";

            }
        }

        /// <summary>
        ///     Checks whether license is verified for specified effect.
        /// </summary>
        public bool IsLicenseVerified(Effect effect)
        {
            Status status = Statuses[(int)effect];
            switch (status)
            {
                case Status.OK:
#if !ZIBRA_EFFECTS_OTP_VERSION
                case Status.NoMaintance:
#endif
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        ///     Checks whether license still has maintenance.
        /// </summary>
        public bool HasMaintenance(Effect effect)
        {
            Status status = Statuses[(int)effect];
            switch (status)
            {
                case Status.OK:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        ///     Callback that is triggered when license status is updated.
        /// </summary>
        public Action OnLicenseStatusUpdate;

#endregion
#region Implementation Details
#if ZIBRA_EFFECTS_OTP_VERSION
        const string LICENSE_KEYS_PREF_KEY_OLD = "ZibraLiquidsLicenceKey";
        const string LICENSE_KEYS_PREF_KEY_OLD2 = "ZibraEffectsLicenceKeyOTP";
        const string LICENSE_KEYS_PREF_KEY = "ZibraEffectsLicenceKeyOTPV2";
#else
        const string LICENSE_KEYS_PREF_KEY_OLD = "ZibraEffectsLicenceKey";
        const string LICENSE_KEYS_PREF_KEY = "ZibraEffectsLicenceKeyV2";
#endif
        const string SESSION_STATE_KEY = "ZibraEffectsLicenceSessionState";

        private const string KEY_FORMAT_REGEX =
            "^(([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})|([0-9A-F]{8}-[0-9A-F]{8}-[0-9A-F]{8}-[0-9A-F]{8}))$";

        private const string VERSION_DATE = "2024.04.29";

        private const string LICENSE_VALIDATION_URL = "https://license.zibra.ai/api/licenseExpiration";

        string[] LicenseKeys = new string[(int)Effect.Count];
        Status[] Statuses = new Status[(int)Effect.Count];
        string[] ServerErrors = new string[(int)Effect.Count];

        struct LicenseSessionState
        {
            public Status[] Statuses;
            public string[] ServerErrors;
        }

        // C# doesn't know we use it with JSON deserialization
#pragma warning disable 0649
        /// @cond SHOW_INTERNAL_JSON_FIELDS
        // Some classes needs to be public for JSON deserialization
        // But it is not intended to be used by end users
        [Serializable]
        public class RandomNumberDeclaration
        {
            public string product;
            public string number;
        }

        [Serializable]
        class LicenseKeyRequest
        {
            public string api_version = "2023.05.17";
            public string[] license_keys;
            public RandomNumberDeclaration[] random_numbers;
            public string hardware_id;
            public string engine;
        }

        [Serializable]
        class LicenseKeyResponse
        {
            public string license_info;
            public string signature;
        }

        [Serializable]
        public class LicenseWarning
        {
            public string header_text;
            public string body_text;
            public string button_text;
            public string URL;
        }

        [Serializable]
        class LicenseInfo
        {
            public string license_key;
            public string license;
            public string latest_version;
            public string random_number;
            public string hardware_id;
            public string engine;
            public string product;
            public string message;
            public LicenseWarning warning;
        }

        [Serializable]
        class LicenseInfoWrapper
        {
            public LicenseInfo[] items;
        }

        [Serializable]
        class ErrorInfo
        {
            public string license_info;
        }

        /// @endcond
#pragma warning restore 0649

        static private LicensingManager _Instance;

        private LicensingManager()
        {
            RestoreLicenseKeys();
            RestoreSessionState();
            ValidateLicense();
        }

        void SaveLicenseKeys()
        {
            string serializedList = ZibraJsonUtil.ToJson(LicenseKeys);
            EditorPrefs.SetString(LICENSE_KEYS_PREF_KEY, serializedList);
        }

        void RestoreLicenseKeys()
        {
#if ZIBRA_EFFECTS_OTP_VERSION
            if (EditorPrefs.HasKey(LICENSE_KEYS_PREF_KEY_OLD))
            {
                string key = EditorPrefs.GetString(LICENSE_KEYS_PREF_KEY_OLD);
                LicenseKeys[(int)Effect.Liquid] = key;
                EditorPrefs.DeleteKey(LICENSE_KEYS_PREF_KEY_OLD);
            }
            if (EditorPrefs.HasKey(LICENSE_KEYS_PREF_KEY_OLD2))
            {
                string key = EditorPrefs.GetString(LICENSE_KEYS_PREF_KEY_OLD2);
                LicenseKeys[(int)Effect.Liquid] = key;
                EditorPrefs.DeleteKey(LICENSE_KEYS_PREF_KEY_OLD2);
            }
#else
            if (EditorPrefs.HasKey(LICENSE_KEYS_PREF_KEY_OLD))
            {
                string key = EditorPrefs.GetString(LICENSE_KEYS_PREF_KEY_OLD);
                LicenseKeys[(int)Effect.Liquid] = key;
                LicenseKeys[(int)Effect.Smoke] = key;
                EditorPrefs.DeleteKey(LICENSE_KEYS_PREF_KEY_OLD);
            }
#endif
            if (EditorPrefs.HasKey(LICENSE_KEYS_PREF_KEY))
            {
                string serializedList = EditorPrefs.GetString(LICENSE_KEYS_PREF_KEY);
                LicenseKeys = ZibraJsonUtil.FromJson<string[]>(serializedList);
            }

            if (LicenseKeys.Length != (int)Effect.Count)
            {
                var oldKeys = LicenseKeys;
                LicenseKeys = new string[(int)Effect.Count];
                for (int i = 0; i < Math.Min(oldKeys.Length, LicenseKeys.Length); i++)
                {
                    LicenseKeys[i] = oldKeys[i];
                }
            }

            for (int i = 0; i < (int)Effect.Count; ++i)
            {
                Effect effect = (Effect)i;
                if (!string.IsNullOrEmpty(GetSavedLicenseKey(effect)))
                {
                    ActivationTracking.GetInstance(effect).SetAutomaticActivation(true);
                }
            }
        }

        void SaveSessionState()
        {
            LicenseSessionState sessionState = new LicenseSessionState();
            sessionState.Statuses = Statuses;
            sessionState.ServerErrors = ServerErrors;
            string serializedState = ZibraJsonUtil.ToJson(sessionState);
            SessionState.SetString(SESSION_STATE_KEY, serializedState);
        }

        void RestoreSessionState()
        {
            if (SessionState.GetString(SESSION_STATE_KEY, "") != "")
            {
                string serializedState = SessionState.GetString(SESSION_STATE_KEY, "");
                LicenseSessionState sessionState = ZibraJsonUtil.FromJson<LicenseSessionState>(serializedState);
                Statuses = sessionState.Statuses;
                ServerErrors = sessionState.ServerErrors;
            }

            for (int i = 0; i < Statuses.Length; i++)
            {
                switch (Statuses[i])
                {
                    case Status.ValidationInProgress:
                    case Status.NotInstalled:
                        Statuses[i] = Status.NotInitialized;
                        break;
                }
            }
        }

        void ValidateEffectStatuses()
        {
            for (int i = 0; i < (int)Effect.Count; ++i)
            {
                Effect effect = (Effect)i;
                if (!PluginManager.IsAvailable(effect))
                {
                    Statuses[i] = Status.NotInstalled;
                }
                if (string.IsNullOrEmpty(LicenseKeys[i]))
                {
                    Statuses[i] = Status.NoKey;
                }
            }
        }

        void SendValidationRequest(List<Effect> toValidate)
        {
            LicenseKeyRequest licenseKeyRequest = FillKeyRequest(toValidate);
            string requestData = ZibraJsonUtil.ToJson(licenseKeyRequest);

#if UNITY_2022_2_OR_NEWER
            UnityWebRequest request = UnityWebRequest.PostWwwForm(LICENSE_VALIDATION_URL, requestData);
#else
            UnityWebRequest request = UnityWebRequest.Post(LICENSE_VALIDATION_URL, requestData);
#endif
            UnityWebRequestAsyncOperation requestOperation = request.SendWebRequest();
            requestOperation.completed += (operation) =>
            {
                UnityWebRequestAsyncOperation requestOperation = operation as UnityWebRequestAsyncOperation;
                UnityWebRequest request = requestOperation.webRequest;
                RequestCompleted(toValidate, request);
            };
        }

        private LicenseKeyRequest FillKeyRequest(List<Effect> toValidate)
        {
            LicenseKeyRequest licenseKeyRequest = new LicenseKeyRequest();
            licenseKeyRequest.license_keys = new string[toValidate.Count];
            licenseKeyRequest.random_numbers = new RandomNumberDeclaration[toValidate.Count];
            licenseKeyRequest.hardware_id = PluginManager.GetHardwareID();
            licenseKeyRequest.engine = "unity";

            // Fill licenseKeyRequest with data from toValidate
            for (int i = 0; i < toValidate.Count; ++i)
            {
                Effect effect = toValidate[i];
                licenseKeyRequest.license_keys[i] = LicenseKeys[(int)effect];
                licenseKeyRequest.random_numbers[i] = new RandomNumberDeclaration();
                licenseKeyRequest.random_numbers[i].product = PluginManager.GetEffectName(effect);
                licenseKeyRequest.random_numbers[i].number = PluginManager.LicensingGetRandomNumber(effect).ToString();
            }

            return licenseKeyRequest;
        }

        void SetStatus(List<Effect> effects, Status status)
        {
            for (int i = 0; i < effects.Count; ++i)
            {
                Effect effect = effects[i];
                Statuses[(int)effect] = status;
            }
            SaveSessionState();
            OnLicenseStatusUpdate?.Invoke();
        }

        void SetStatus(Effect effect, Status status)
        {
            Statuses[(int)effect] = status;
            SaveSessionState();
            OnLicenseStatusUpdate?.Invoke();
        }

        void RequestCompleted(List<Effect> toValidate, UnityWebRequest request)
        {
            if (request.result == UnityWebRequest.Result.Success)
            {
                ProcessServerResponse(toValidate, request.downloadHandler.text);
            }
            else
            {
                SetStatus(toValidate, Status.NetworkError);
                Debug.LogError("Zibra License Key validation error: " + request.error + "\n" +
                    request.downloadHandler.text);
            }
            request.Dispose();
        }

        void ProcessServerResponse(List<Effect> toValidate, string response)
        {
            LicenseKeyResponse parsedResponse = ZibraJsonUtil.FromJson<LicenseKeyResponse>(response);
            if (parsedResponse.signature == null || parsedResponse.license_info == null)
            {
                SetStatus(toValidate, Status.NetworkError);
                return;
            }

            if (CheckResponseForError(toValidate, parsedResponse))
            {
                return;
            }

            LicenseInfo[] licenseInfos = null;
            if (!DeserializeLicenseInfo(toValidate, parsedResponse, ref licenseInfos))
            {
                return;
            }

            foreach (LicenseInfo licenseInfo in licenseInfos)
            {
                ParseLicenseInfo(toValidate, licenseInfo, response);
            }

            VSPAttribution(toValidate);

            for (int i = 0; i < (int)Effect.Count; ++i)
            {
                Effect effect = (Effect)i;
                if (IsLicenseVerified(effect))
                {
                    ActivationTracking.GetInstance(effect).TrackActivation();
                }
            }
        }

        private void VSPAttribution(List<Effect> toValidate)
        {
#if !ZIBRA_EFFECTS_OTP_VERSION
            bool isActivated = false;
            foreach (Effect effect in toValidate)
            {
                if (IsLicenseVerified(effect))
                {
                    isActivated = true;
                    break;
                }
            }

            if (!isActivated)
            {
                return;
            }

            string licenseKeys = string.Join(',', GetSavedLicenseKeys());
            if (!string.IsNullOrEmpty(licenseKeys))
            {
                UnityEditor.VSAttribution.ZibraAI.VSAttribution.SendAttributionEvent("ZibraEffects_Login", "ZibraAI", licenseKeys);
            }
#endif
        }

        void ParseLicenseInfo(List<Effect> toValidate, LicenseInfo licenseInfo, string response)
        {
            Effect effect = PluginManager.ParseEffectName(licenseInfo.product);

            if (!toValidate.Contains(effect))
            {
                return;
            }

            // Unity's JsonUtility may create empty, non-null licenseInfo.warning
            // Need to check whether we have at least some data in licenseInfo.warning
            if (licenseInfo.warning != null && licenseInfo.warning.header_text != null)
            {
                LicenseWarning warning = licenseInfo.warning;
                Debug.LogWarning(warning.header_text + "\n" + warning.body_text);
                ZibraEffectsMessagePopup.OpenMessagePopup(warning.header_text, warning.body_text, warning.URL, warning.button_text);
            }

            switch (licenseInfo.license)
            {
                case "ok":
                    SetStatus(effect, Status.OK);
                    break;
#if !ZIBRA_EFFECTS_OTP_VERSION
                case "old_version_only":
                    int comparison = String.Compare(licenseInfo.latest_version, VERSION_DATE, StringComparison.Ordinal);
                    if (comparison < 0)
                    {
                        SetStatus(effect, Status.Expired);
                    }
                    else
                    {
                        SetStatus(effect, Status.NoMaintance);
                    }
                    break;
                case "expired":
                    SetStatus(effect, Status.Expired);
                    break;
#endif
                default:
                    SetStatus(effect, Status.ValidationError);
                    break;
            }

            if (IsLicenseVerified(effect))
            {
                PluginManager.ValidateLicense(effect, response);
            }
        }

        bool DeserializeLicenseInfo(List<Effect> toValidate, LicenseKeyResponse parsedResponse, ref LicenseInfo[] licenseInfos)
        {
            try
            {
                licenseInfos = ZibraJsonUtil.FromJson<LicenseInfo[]>(parsedResponse.license_info);
            }
            catch (Exception)
            {
                for (int i = 0; i < toValidate.Count; ++i)
                {
                    Effect effect = toValidate[i];
                    ServerErrors[(int)effect] = "Invalid Key.";
                }
                SetStatus(toValidate, Status.ValidationError);
                return false;
            }
            return true;
        }

        bool CheckResponseForError(List<Effect> toValidate, LicenseKeyResponse parsedResponse)
        {
            try
            {
                ErrorInfo errorInfo = ZibraJsonUtil.FromJson<ErrorInfo>(parsedResponse.license_info);
                if (errorInfo != null)
                {
                    for (int i = 0; i < toValidate.Count; ++i)
                    {
                        Effect effect = toValidate[i];
                        ServerErrors[(int)effect] = errorInfo.license_info;
                    }
                    SetStatus(toValidate, Status.ValidationError);
                    return true;
                }
            }
            catch (Exception)
            {
                // No errors reported
            }
            return false;
        }

#endregion
    }
}
