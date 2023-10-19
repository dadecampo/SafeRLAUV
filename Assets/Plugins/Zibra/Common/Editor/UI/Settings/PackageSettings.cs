using com.zibra.common;

namespace com.zibra.liquid
{
    internal class ZibraAiPackageInfo : IPackageInfo
    {
        public string displayName => "Zibra Effects";
        public string description =>
            "Solution for the Unity engine. It allows the use of GPU accelerated real-time simulated interactive effects powered by AI based object approximation.";
        public string version => Effects.Version;
    }
}
