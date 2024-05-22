#if UNITY_EDITOR && ZIBRA_EFFECTS_OTP_VERSION

using com.zibra.common.Editor.Licensing;
using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using static com.zibra.common.Editor.PluginManager;

namespace com.zibra.common.Editor.SDFObjects
{
    /// <summary>
    ///     Class responsible for managing registration of OTP version.
    /// </summary>
    [DisallowMultipleComponent]
    [ExecuteInEditMode]
    [InitializeOnLoad]
    class RegistrationManager
    {
        /// <summary>
        ///     Possible registration statuses.
        /// </summary>
        public enum Status
        {
            NotRegistered,
            InProgress,
            NetworkError,
            InvalidData,
            OK
        }

        /// <summary>
        ///     Current registration status.
        /// </summary>
        public Status CurrentStatus { get; private set; } = Status.NotRegistered;
        public static RegistrationManager Instance { get; private set; } = new RegistrationManager();
        public bool IsFirstRegistration { get; private set; } = false;
        public RegistrationManager()
        {
        }

        public static void Reset()
        {
            Instance = new RegistrationManager();
        }

        /// <summary>
        ///     Human readable error description.
        /// </summary>
        public string ErrorMessage { get; private set; } = "";

        /// <summary>
        ///     Method that tries to register the plugin.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         Does nothing if license is already validated.
        ///     </para>
        ///     <para>
        ///         Will activate newly registered key on success.
        ///     </para>
        /// </remarks>
        public void Register(Effect effect, string OrderNumber, string Email, string Name)
        {
            if (LicensingManager.Instance.IsLicenseVerified(effect))
            {
                CurrentStatus = Status.OK;
                return;
            }

            RegistrationData data = new RegistrationData();
            data.order = OrderNumber;
            data.email = Email;
            data.name = Name;
            data.product = PluginManager.GetEffectName(effect);

            string json = JsonUtility.ToJson(data);

#if UNITY_2022_2_OR_NEWER
            UnityWebRequest request = UnityWebRequest.PostWwwForm(URL, json);
#else
            UnityWebRequest request = UnityWebRequest.Post(URL, json);
#endif

            CurrentStatus = Status.InProgress;
            ErrorMessage = "Registration in progress";
            UnityWebRequestAsyncOperation requestOperation = request.SendWebRequest();
            requestOperation.completed += (operation) =>
            {
                UnityWebRequestAsyncOperation requestOperation = operation as UnityWebRequestAsyncOperation;
                UnityWebRequest request = requestOperation.webRequest;
                UpdateRequest(effect, request);
            };
        }

        private const string URL = "https://generation.zibra.ai/api/apiKey?type=registration";
        [Serializable]
        private struct RegistrationData
        {
            public string email;
            public string name;
            public string order;
            public string product;
        }

        [Serializable]
        private struct RegistrationResponse
        {
            public string[] api_keys;
            public bool is_first_registration;
        }

        private void UpdateRequest(Effect effect, UnityWebRequest request)
        {
            var result = request.downloadHandler.text;
            if (result != null && request.result == UnityWebRequest.Result.Success)
            {
                ProcessServerResponse(effect,result);
            }
            else if (request.result != UnityWebRequest.Result.Success)
            {
                CurrentStatus = request.result == UnityWebRequest.Result.ProtocolError ? Status.InvalidData : Status.NetworkError;
                ErrorMessage = request.downloadHandler.text;
                if (ErrorMessage == null || ErrorMessage == "")
                {
                    ErrorMessage = "Network error. Please ensure you are connected to the Internet and try again.";
                }
                Debug.LogError("Zibra Registration error: " + request.error + "\n" +
                                request.downloadHandler.text);
            }
            request.Dispose();
        }

        private void ProcessServerResponse(Effect effect, string response)
        {
            RegistrationResponse parsedResponse = JsonUtility.FromJson<RegistrationResponse>(response);
            if (parsedResponse.api_keys != null && parsedResponse.api_keys.Length > 0)
            {
                IsFirstRegistration = parsedResponse.is_first_registration;
                CurrentStatus = Status.OK;
                ErrorMessage = "";
                LicensingManager.Instance.ValidateLicense(parsedResponse.api_keys[0], effect);
            }
            else
            {
                CurrentStatus = Status.InvalidData;
                ErrorMessage = "Invalid data provided.";
            }
        }
    }
}

#endif
