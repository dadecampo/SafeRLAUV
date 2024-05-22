namespace com.zibra.common
{
    internal interface IPackageInfo
    {
        string displayName { get; }
        string description { get; }
        string version { get; }
        string distributionType { get; }
    }
}
