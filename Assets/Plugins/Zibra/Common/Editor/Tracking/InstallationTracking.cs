using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

using com.zibra.smoke_and_fire.Bridge;

namespace com.zibra.common.Editor
{
    internal static class InstallationTracking
    {
        const string IS_TRACKED_PREFS_KEY = "ZibraEffectsIsInstallationTracked";
        const string BASE_URL = "https://license.zibra.ai/api/installationTracking";
        const int SUCCESS_CODE = 200;
        static UnityWebRequestAsyncOperation Request;

        [InitializeOnLoadMethod]
        static void OnLoad()
        {
            // Don't track something that is potentially build-machine
            if (Application.isBatchMode)
            {
                return;
            }

            if (!EditorPrefs.GetBool(IS_TRACKED_PREFS_KEY, false))
            {
                ReportInstallation();
            }
        }

        static void ReportInstallation()
        {
            string URL = BASE_URL + "?product=effects" + "&hardware_id=" + SmokeAndFireBridge.GetHardwareIDWrapper();

            Request = UnityWebRequest.Get(URL).SendWebRequest();
            Request.completed += UpdateRequest;
        }

        static void UpdateRequest(AsyncOperation obj)
        {
            if (Request == null || !Request.isDone)
            {
                return;
            }

            if (Request.webRequest.result != UnityWebRequest.Result.Success)
            {
                Request.webRequest.Dispose();
                Request = null;
                return;
            }

            if (Request.webRequest.responseCode != SUCCESS_CODE)
            {
                Request.webRequest.Dispose();
                Request = null;
                return;
            }

            EditorPrefs.SetBool(IS_TRACKED_PREFS_KEY, true);
            Request.webRequest.Dispose();
            Request = null;
        }
    }
}
