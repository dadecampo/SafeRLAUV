#if UNITY_EDITOR

using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.Text.RegularExpressions;
using com.zibra.liquid.Bridge;
using com.zibra.smoke_and_fire.Bridge;

namespace com.zibra.common.Editor.SDFObjects
{
    /// <summary>
    ///     Class responsible for managing licensing and allowing server communication.
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    [InitializeOnLoad]
    public class ServerAuthManager
    {
#region Public Interface

        /// <summary>
        ///     Effect types.
        /// </summary>
        public enum Effect
        {
            Liquids = 0,
            Smoke = 1,
            Count = 2
        }

        /// <summary>
        ///     Status of license validation.
        /// </summary>
        public enum Status
        {
            NotInitialized = 0,
            OK,
            NoMaintance,
            KeyValidationInProgress,
            NetworkError,
            Expired,
            InvalidKey,
            NotRegistered
        }

        /// <summary>
        ///     License key used for the plugin.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Not necessarily correct.
        ///         Use <see cref="IsLicenseVerified"/> to check for that.
        ///     </para>
        ///     <para>
        ///         May be empty.
        ///     </para>
        /// </remarks>
        public string[] PluginLicenseKeys { get; private set; } = new string[0];

        /// <summary>
        ///     License key used for the generation.
        /// </summary>
        /// <remarks>
        ///     It's either validated key suitable for generation or an empty string.
        /// </remarks>
        public string GenerationLicenseKey { get; private set; } = "";

        /// <summary>
        ///     URL address for generation API.
        /// </summary>
        /// <remarks>
        ///     Includes <see cref="GenerationLicenseKey"/> in the URL.
        /// </remarks>
        public string GenerationURL { get; private set; } = "";

        /// <summary>
        ///     Returns current status of license validation for specified effect.
        /// </summary>
        /// <remarks>
        ///     Never returns NotInitialized, since it initialized validation in this case.
        /// </remarks>
        public Status GetStatus(Effect effect)
        {
            return CurrentStatus[(int)effect];
        }

        private void SetStatus(Status newStatus, Effect effect)
        {
            SessionState.SetInt(STATUS_SESSION_KEY + effect, (int)newStatus);
            CurrentStatus[(int)effect] = newStatus;
        }

        private void SetStatusGlobal(Status newStatus)
        {
            for (int j = 0; j < (int)Effect.Count; ++j)
            {
                SetStatus(newStatus, (Effect)j);
            }
        }

        /// <summary>
        ///     Checks whether license is verified for specified effect.
        /// </summary>
        /// <returns>
        ///     True if license is valid, false otherwise.
        /// </returns>
        public bool IsLicenseVerified(Effect effect)
        {
            switch (GetStatus(effect))
            {
            case Status.OK:
            case Status.NoMaintance:
                return true;
            default:
                return false;
            }
        }

        /// <summary>
        ///     Checks whether specified string is correctly formated list of keys.
        /// </summary>
        public static bool CheckKeysFormat(string keys)
        {
            if (keys.Trim().Length == 0)
                return false;

            foreach (string key in keys.Split(','))
            {
                if (!Regex.IsMatch(key.Trim(), KEY_FORMAT_REGEX))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     Checks whether generation is available.
        /// </summary>
        /// <returns>
        ///     True if license is valid and not expired, false otherwise.
        /// </returns>
        public bool IsGenerationAvailable()
        {
            return GenerationLicenseKey != "";
        }

        /// <summary>
        ///     Returns human readable string explaining error based on <see cref="Status"/>.
        /// </summary>
        public string GetErrorMessage(Status status)
        {
            switch (status)
            {
            case Status.KeyValidationInProgress:
                return "License key validation in progress. Please wait.";
            case Status.NetworkError:
                return "Network error. Please try again later.";
            case Status.InvalidKey:
                return "License key is invalid.";
            case Status.Expired:
                return "License expired.";
            case Status.NotRegistered:
                return "Plugin is not registered.";
            default:
                return "";
            }
        }

        /// <summary>
        ///     Returns human readable string explaining current error with activation of specified effect.
        /// </summary>
        public string GetErrorMessage(Effect effect)
        {
            return GetErrorMessage(GetStatus(effect));
        }

        /// <summary>
        ///     Returns human readable string explaining current error with generation availability.
        /// </summary>
        public string GetGenerationErrorMessage()
        {
            if (GenerationLicenseKey != "")
            {
                return "";
            }

            int highestPriorityError = (int)Status.NotRegistered;
            for (int i = 0; i < (int)Effect.Count; ++i)
            {
                highestPriorityError = Math.Min(highestPriorityError, (int)CurrentStatus[i]);
            }
            return GetErrorMessage((Status)highestPriorityError);
        }

        /// <summary>
        ///     Returns singleton of this class.
        /// </summary>
        /// <remarks>
        ///     Creates and initializes instance if needed.
        /// </remarks>
        public static ServerAuthManager GetInstance()
        {
            return Instance;
        }

        /// <summary>
        ///     Sets license key and starts key validation process.
        /// </summary>
        public void RegisterKey(string key)
        {
            ValidateLicense(key.Split(','));
        }

        /// <summary>
        ///     Removes current license key.
        /// </summary>
        ///
        public void RemoveKey()
        {
            EditorPrefs.DeleteKey(LICENSE_KEYS_PREF_KEY);
            PluginLicenseKeys = new string[0];
            SetStatusGlobal(Status.NotRegistered);
        }

#endregion
#region Implementation details
        /// <summary>
        ///     Restores state after domain reload
        /// </summary>
        /// <returns>
        ///     true if at least 1 effect need license validation.
        /// </returns>
        private bool RestoreSessionState()
        {
            bool needValidation = false;
            for (int i = 0; i < (int)Effect.Count; ++i)
            {
                Effect effect = (Effect)i;
                Status restoredStatus = (Status)SessionState.GetInt(STATUS_SESSION_KEY + effect, 0);
                switch (restoredStatus)
                {
                case Status.NotInitialized:
                case Status.KeyValidationInProgress:
                case Status.NetworkError:
                    restoredStatus = Status.NotInitialized;
                    needValidation = true;
                    break;
                }
                CurrentStatus[i] = restoredStatus;
            }
            GenerationLicenseKey = SessionState.GetString(GENERATION_KEY_SESSION_KEY, "");
            return needValidation;
        }

        private ServerAuthManager()
        {
            HardwareID = LiquidBridge.GetHardwareIDWrapper();
            PluginLicenseKeys = GetEditorPrefsLicenceKey();

            bool needValidation = RestoreSessionState();
            if (!needValidation)
            {
                GenerationURL = CreateGenerationRequestURL("compute");
                return;
            }

            ValidateLicense(PluginLicenseKeys);
        }

        static ServerAuthManager()
        {
            Instance = new ServerAuthManager();
        }

        private const string BASE_URL = "https://generation.zibra.ai/";
        private const string LICENSE_VALIDATION_URL = "https://license.zibra.ai/api/licenseExpiration";
        private const string VERSION_DATE = "2023.05.22";
        private const string LICENSE_KEYS_PREF_KEY = "ZibraEffectsLicenceKey";
        private const string LICENSE_KEYS_OLD_PREF_KEY = "ZibraLiquidsLicenceKey";
        private const string STATUS_SESSION_KEY = "ZibraEffectsLicenseStatus";
        private const string GENERATION_KEY_SESSION_KEY = "ZibraEffectsGenerationKey";
        private const string KEY_FORMAT_REGEX =
            "^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$";
        private string HardwareID = "";
        private UnityWebRequestAsyncOperation Request;

        private Status[] CurrentStatus = new Status[(int)Effect.Count];

        private static ServerAuthManager Instance = null;

        internal delegate void OnProLicenseWarningCallback(string headerText, string bodyText, string url,
                                                           string buttonText);
        internal static OnProLicenseWarningCallback OnProLicenseWarning;

        private string GetRandomNumber(Effect effect)
        {
#if !ZIBRA_EFFECTS_NO_LICENSE_CHECK
            switch (effect)
            {
            case Effect.Liquids:
                return LiquidBridge.ZibraLiquid_GetRandomNumber().ToString();
            case Effect.Smoke:
                return SmokeAndFireBridge.ZibraSmokeAndFire_GetRandomNumber().ToString();
            }
            throw new Exception($"Invalid argument {effect} passed to GetRandomNumber");
#else
            return "0";
#endif
        }

        private void ValidateLicense(string[] keys)
        {
            if (keys.Length == 0)
            {
                SetStatusGlobal(Status.NotRegistered);
                return;
            }

            SetLicenceKeys(keys);

            LicenseKeyRequest licenseKeyRequest = new LicenseKeyRequest();
            licenseKeyRequest.license_keys = keys;
            licenseKeyRequest.random_numbers = new RandomNumberDeclaration[(int)Effect.Count];
            licenseKeyRequest.hardware_id = HardwareID;
            licenseKeyRequest.engine = "unity";

            for (int i = 0; i < (int)Effect.Count; ++i)
            {
                Effect effect = (Effect)i;
                licenseKeyRequest.random_numbers[i] = new RandomNumberDeclaration();
                var currentRandomNumberDeclaration = licenseKeyRequest.random_numbers[i];
                currentRandomNumberDeclaration.product = Enum.GetName(typeof(Effect), effect).ToLower();
                currentRandomNumberDeclaration.number = GetRandomNumber(effect);
            }

            string json = JsonUtility.ToJson(licenseKeyRequest);

            UnityWebRequest postRequest;
#if UNITY_2022_2_OR_NEWER
            postRequest = UnityWebRequest.PostWwwForm(LICENSE_VALIDATION_URL, json);
#else
            postRequest = UnityWebRequest.Post(LICENSE_VALIDATION_URL, json);
#endif
            Request = postRequest.SendWebRequest();
            Request.completed += UpdateLicenseRequest;
            SetStatusGlobal(Status.KeyValidationInProgress);
        }

        private string[] GetEditorPrefsLicenceKey()
        {
            if (EditorPrefs.HasKey(LICENSE_KEYS_PREF_KEY))
            {
                string pref_keys = EditorPrefs.GetString(LICENSE_KEYS_PREF_KEY);
                return pref_keys.Split(',');
            }

            if (EditorPrefs.HasKey(LICENSE_KEYS_OLD_PREF_KEY))
            {
                string pref_keys = EditorPrefs.GetString(LICENSE_KEYS_OLD_PREF_KEY);
                EditorPrefs.SetString(LICENSE_KEYS_PREF_KEY, pref_keys);
                return pref_keys.Split(',');
            }

            return new string[0];
        }

        // C# doesn't know we use it with JSON deserialization
#pragma warning disable 0649
        /// @cond SHOW_INTERNAL_JSON_FIELDS
        [Serializable]
        public class RandomNumberDeclaration
        {
            public string product;
            public string number;
        }

        [Serializable]
        public class LicenseKeyRequest
        {
            public string api_version = "2023.05.17";
            public string[] license_keys;
            public RandomNumberDeclaration[] random_numbers;
            public string hardware_id;
            public string engine;
        }

        [Serializable]
        public class LicenseKeyResponse
        {
            public string license_info;
            public string signature;
        }

        // LicenseWarning needs to be public for JSON deserialization
        // But it is not intended to be used by end users
        [Serializable]
        public class LicenseWarning
        {
            public string header_text;
            public string body_text;
            public string button_text;
            public string URL;
        }

        [Serializable]
        public class LicenseInfo
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

        // Unity's built-in JSON parser can't process top level arrays
        // So we need to wrap them into another object and wrap json string into object
        public class LicenseInfoWrapper
        {
            public LicenseInfo[] items;
        }

/// @endcond
#pragma warning restore 0649

        private void ActivateNativePlugin(string serverResponse, Effect effect)
        {
#if !ZIBRA_EFFECTS_NO_LICENSE_CHECK
            switch (effect)
            {
            case Effect.Liquids:
                LiquidBridge.ZibraLiquid_ValidateLicense(serverResponse, serverResponse.Length);
                break;
            case Effect.Smoke:
                SmokeAndFireBridge.ZibraSmokeAndFire_ValidateLicense(serverResponse, serverResponse.Length);
                break;
            default:
                throw new Exception($"Invalid argument {effect} passed to GetRandomNumber");
            }
#endif
        }

        private void ProcessServerResponse(string response)
        {
            LicenseKeyResponse parsedResponse = JsonUtility.FromJson<LicenseKeyResponse>(response);
            if (parsedResponse.signature == null || parsedResponse.license_info == null)
            {
                SetStatusGlobal(Status.InvalidKey);
                return;
            }

            LicenseInfoWrapper licenseInfoWrapper =
                JsonUtility.FromJson<LicenseInfoWrapper>("{\"items\":" + parsedResponse.license_info + "}");
            LicenseInfo[] licenseInfos = licenseInfoWrapper.items;

            bool activatedPlugin = false;

            if (licenseInfos == null)
            {
                SetStatusGlobal(Status.InvalidKey);
                return;
            }

            foreach (LicenseInfo info in licenseInfos)
            {
                Effect effect;
                bool effectDetected = Enum.TryParse<Effect>(info.product, true, out effect);
                if (!effectDetected)
                {
                    continue;
                }

                Debug.Log($"Zibra Effects {effect} License Info: {info.message}");

                // Unity's JsonUtility may create empty, non-null licenseInfo.warning
                // Need to check whether we have at least some data in licenseInfo.warning
                if (info.warning != null && info.warning.header_text != null)
                {
                    LicenseWarning warning = info.warning;
                    Debug.LogWarning(warning.header_text + "\n" + warning.body_text);
                    OnProLicenseWarning(warning.header_text, warning.body_text, warning.URL, warning.button_text);
                }
                switch (info.license)
                {
                case "ok":
                    SetStatus(Status.OK, effect);
                    GenerationLicenseKey = info.license_key;
                    break;
                case "old_version_only":
                    int comparison = String.Compare(info.latest_version, VERSION_DATE, StringComparison.Ordinal);
                    if (comparison < 0)
                    {
                        SetStatus(Status.Expired, effect);
                        continue;
                    }
                    SetStatus(Status.NoMaintance, effect);
                    break;
                case "expired":
                    SetStatus(Status.Expired, effect);
                    continue;
                default:
                    SetStatus(Status.InvalidKey, effect);
                    continue;
                }

                activatedPlugin = true;
                ActivateNativePlugin(response, effect);
            }

            if (!activatedPlugin)
            {
                return;
            }

            UnityEditor.VSAttribution.ZibraAI.VSAttribution.SendAttributionEvent("ZibraEffects_Login", "ZibraAI",
                                                                                 String.Join(",", PluginLicenseKeys));
            // populate server request URL if everything is fine
            SessionState.SetString(GENERATION_KEY_SESSION_KEY, GenerationLicenseKey);
            GenerationURL = CreateGenerationRequestURL("compute");
            Request = null;
        }

        private void UpdateLicenseRequest(AsyncOperation obj)
        {
            if (Request == null)
            {
                return;
            }

            if (Request.isDone)
            {
                var result = Request.webRequest.downloadHandler.text;
#if UNITY_2020_2_OR_NEWER
                if (result != null && Request.webRequest.result == UnityWebRequest.Result.Success)
#else
                if (result != null && !Request.webRequest.isHttpError && !Request.webRequest.isNetworkError)
#endif
                {
                    ProcessServerResponse(result);
                }
#if UNITY_2020_2_OR_NEWER
                else if (Request.webRequest.result != UnityWebRequest.Result.Success)
#else
                else if (Request.webRequest.isHttpError || Request.webRequest.isNetworkError)
#endif
                {
                    SetStatusGlobal(Status.NetworkError);
                    Debug.LogError("Zibra Effects License Key validation error: " + Request.webRequest.error + "\n" +
                                   Request.webRequest.downloadHandler.text);
                }
            }
            return;
        }

        private void SetLicenceKeys(string[] keys)
        {
            PluginLicenseKeys = keys;
            EditorPrefs.SetString(LICENSE_KEYS_PREF_KEY, String.Join(",", keys));
        }

        private string CreateGenerationRequestURL(string type)
        {
            string generationURL;

            generationURL = BASE_URL + "api/unity/" + type + "?";

            if (HardwareID != "")
            {
                generationURL += "&hardware_id=" + HardwareID;
            }

            if (GenerationLicenseKey != "")
            {
                generationURL += "&api_key=" + GenerationLicenseKey;
            }

            return generationURL;
        }
#endregion
    }
}

#endif
