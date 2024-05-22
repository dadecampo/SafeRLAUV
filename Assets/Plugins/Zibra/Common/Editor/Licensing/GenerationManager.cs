#if UNITY_EDITOR

using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.Text.RegularExpressions;
using static com.zibra.common.Editor.PluginManager;

namespace com.zibra.common.Editor.Licensing
{

    /// <summary>
    ///     Class responsible for managing generation authentication.
    /// </summary>
    [InitializeOnLoad]
    public class GenerationManager
    {
#region Public Interface

        /// <summary>
        ///     Instance of generation manager.
        /// </summary>
        public static GenerationManager Instance { get; private set; } = new GenerationManager();

        /// <summary>
        ///     Checks whether generation is available.
        /// </summary>
        public bool IsGenerationAvailable()
        {
            return !string.IsNullOrEmpty(GetGenerationKey());
        }

        /// <summary>
        ///     Checks whether user needs to activate the plugin for generation to work.
        /// </summary>
        /// <remarks>
        ///     Generation may not be available due to network error or validation may still be in progress.
        ///     It only makes sense to ask user to activate the plugin only for subset for validation errors.
        /// </remarks>
        public bool NeedActivation()
        {
            for (int i = 0; i < (int)Effect.Count; ++i)
            {
                Effect effect = (Effect)i;
                LicensingManager.Status licenseStatus = LicensingManager.Instance.GetStatus(effect);
                if (licenseStatus == LicensingManager.Status.NoKey || licenseStatus == LicensingManager.Status.ValidationError)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///     If generation is available, returns URL to generation backend endpoint.
        /// </summary>
        public string GetGenerationURL()
        {
            if (!IsGenerationAvailable())
            {
                throw new Exception("Generation is not available.");
            }

            return $"https://generation.zibra.ai/api/unity/compute?hardware_id={GetHardwareID()}&api_key={GetGenerationKey()}";
        }

        /// <summary>
        ///     Returns human readable error describing why generation is not available.
        /// </summary>
        public string GetErrorMessage()
        {
            for (int i = 0; i < (int)Effect.Count; ++i)
            {
                Effect effect = (Effect)i;
                if (IsAvailable(effect))
                {
                    return LicensingManager.Instance.GetErrorMessage(effect);
                }
            }
            return "No effects installed.";
        }

#endregion
#region Implementation details
        string GetGenerationKey()
        {
            LicensingManager.Instance.ValidateLicense();

            for (int i = 0; i < (int)Effect.Count; ++i)
            {
                Effect effect = (Effect)i;
                if (LicensingManager.Instance.HasMaintenance(effect))
                {
                    return LicensingManager.Instance.GetSavedLicenseKey(effect);
                }
            }
            return "";
        }
#endregion
    }
}
#endif
