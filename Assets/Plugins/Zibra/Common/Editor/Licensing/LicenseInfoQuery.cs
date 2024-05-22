using com.zibra.common.Utilities;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using static com.zibra.common.Editor.PluginManager;

namespace com.zibra.common.Editor.Licensing
{
    /// <summary>
    ///     Class that allow to query various info about license.
    /// </summary>
    class LicenseInfoQuery
    {
#region Public Interface
        /// <summary>
        ///     Instance of licensing manager.
        /// </summary>
        public static LicenseInfoQuery Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new LicenseInfoQuery();
                }
                return _Instance;
            }
        }

        /// <summary>
        ///     Returns human readable string describing info about license.
        /// </summary>
        public string GetInfo(Effect effect)
        {
            if (!LicensingManager.Instance.IsLicenseVerified(effect))
            {
                return "";
            }

            if (ErrorMessages[(int)effect] != null)
            {
                return ErrorMessages[(int)effect];
            }

            LicenseInfo licenseInfo = GetLicenseInfo(effect);
            if (licenseInfo == null)
            {
                return "Please wait.\n";
            }

            return $"Seats: {licenseInfo.seats}\nAllowed computers limit: {licenseInfo.hardware_allowed}\nComputers activated on: {licenseInfo.hardware_activated}\n";
        }

        /// <summary>
        ///     Returns true if license is activated on all available unique computers.
        /// </summary>
        public bool IsHardwareLimitHit(Effect effect)
        {
            if (!LicensingManager.Instance.IsLicenseVerified(effect))
            {
                return false;
            }

            if (ErrorMessages[(int)effect] != null)
            {
                return false;
            }

            LicenseInfo licenseInfo = GetLicenseInfo(effect);
            if (licenseInfo == null)
            {
                return false;
            }

            return licenseInfo.hardware_allowed == licenseInfo.hardware_activated;
        }

        /// <summary>
        ///     Resets cached license information.
        /// </summary>
        public void Reset()
        {
            for (int i = 0; i < (int)Effect.Count; ++i)
            {
                Effect effect = (Effect)i;

                CachedInfo[i] = null;
                ErrorMessages[i] = null;
                string sessionStateKey = SAVED_LICENSE_INFO_SESSION_STATE_KEY + PluginManager.GetEffectName(effect);
                SessionState.SetString(sessionStateKey, null);
            }
        }

        /// <summary>
        ///     Callback that is triggered when license info is updated.
        /// </summary>
        public Action OnLicenseInfoUpdate;
#endregion
#region Implementation Details
        const string SAVED_LICENSE_INFO_SESSION_STATE_KEY = "ZibraEffectsSavedLicenseInfo";
        const string URL = "https://license.zibra.ai/api/license/viewer";

        // C# doesn't know we use it with JSON deserialization
#pragma warning disable 0649
        [Serializable]
        class LicenseInfo
        {
            public string license_key;
            public int seats;
            public int hardware_allowed;
            public int hardware_activated;
        }
#pragma warning restore 0649

        LicenseInfo[] CachedInfo = new LicenseInfo[(int)Effect.Count];
        bool[] IsInProgress = new bool[(int)Effect.Count];
        string[] ErrorMessages = new string[(int)Effect.Count];

        LicenseInfo GetLicenseInfo(Effect effect)
        {
            if (CachedInfo[(int)effect] != null)
            {
                return CachedInfo[(int)effect];
            }

            string sessionStateKey = SAVED_LICENSE_INFO_SESSION_STATE_KEY + PluginManager.GetEffectName(effect);
            string sessionState = SessionState.GetString(sessionStateKey, null);
            if (!string.IsNullOrEmpty(sessionState))
            {
                CachedInfo[(int)effect] = JsonUtility.FromJson<LicenseInfo>(sessionState);
                return CachedInfo[(int)effect];
            }

            RequestLicenseInfo(effect);

            return null;
        }

        void RequestLicenseInfo(Effect effect)
        {
            if (IsInProgress[(int)effect])
            {
                return;
            }

            IsInProgress[(int)effect] = true;

            string fullURL = URL + "?license_key=" + LicensingManager.Instance.GetSavedLicenseKey(effect);
            UnityWebRequest request = UnityWebRequest.Get(fullURL);

            UnityWebRequestAsyncOperation requestOperation = request.SendWebRequest();
            requestOperation.completed += (operation) =>
            {
                UnityWebRequestAsyncOperation requestOperation = operation as UnityWebRequestAsyncOperation;
                UnityWebRequest request = requestOperation.webRequest;
                RequestCompleted(effect, request);
            };
        }

        void RequestCompleted(Effect effect, UnityWebRequest request)
        {
            IsInProgress[(int)effect] = false;

            if (request.result == UnityWebRequest.Result.Success)
            {
                ProcessServerResponse(effect, request.downloadHandler.text);
            }
            else
            {
                ErrorMessages[(int)effect] = request.error + "\n" +
                    request.downloadHandler.text + "\n";
            }
            request.Dispose();
        }

        private void ProcessServerResponse(Effect effect, string response)
        {
            LicenseInfo parsedResponse = ZibraJsonUtil.FromJson<LicenseInfo>(response);
            if (parsedResponse == null)
            {
                ErrorMessages[(int)effect] = "Failed to parse server response.\n";
                return;
            }

            string sessionStateKey = SAVED_LICENSE_INFO_SESSION_STATE_KEY + PluginManager.GetEffectName(effect);
            SessionState.SetString(sessionStateKey, response);
            CachedInfo[(int)effect] = parsedResponse;

            OnLicenseInfoUpdate?.Invoke();
        }

        static private LicenseInfoQuery _Instance;
#endregion
    }
}
