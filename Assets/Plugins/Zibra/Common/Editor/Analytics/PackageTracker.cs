using UnityEditor;

namespace com.zibra.common.Analytics
{
    internal static class PackageTracker
    {
        public static bool IsPackageInstalled(string packageName)
        {
            foreach (var package in UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages())
            {
                if (package.name == packageName)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
