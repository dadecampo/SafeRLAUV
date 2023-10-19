using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace com.zibra.common
{
    /// <summary>
    ///     Class containing static data about currently installed version of Zibra Effects.
    /// </summary>
    public static class Effects
    {
        /// <summary>
        ///     Zibra Effects version in form that follow c# versioning standard (d.d.d.d).
        /// </summary>
        /// <remarks>
        ///     This is the version that liquid assemblies will have
        /// </remarks>
        public const string VersionStandard = "2.0.0.0";

        /// <summary>
        ///     Zibra Effects version in human readable form.
        /// </summary>
        /// <remarks>
        ///     <para>
        ///         This is the version that used in UI/Diagnostics info/etc.
        ///     </para>
        ///     <para>
        ///         May contain arbitrary text depending on the version.
        ///     </para>
        /// </remarks>
        public const string Version = "2.0.0";

        /// <summary>
        ///     Signals whether current Zibra Effects version is pre-release one.
        /// </summary>
        /// <remarks>
        ///     Release packages always have this set to false.
        ///     Will only be true for Early Access and similar packages.
        /// </remarks>
        public const bool IsPreReleaseVersion = false;

        /// <summary>
        ///     Base path used by all menu bar items in Zibra Effects.
        /// </summary>
        public const string BaseMenuBarPath = "Zibra Effects/";

        /// <summary>
        ///     Base path used by all Liquids object menu items in Zibra Effects.
        /// </summary>
        public const string LiquidsGameObjectMenuPath = "GameObject/Zibra Effects - Liquids/";

        /// <summary>
        ///     Base path used by all Smoke & Fire object menu items in Zibra Effects.
        /// </summary>
        public const string SmokeAndFireGameObjectMenuPath = "GameObject/Zibra Effects - Smoke and Fire/";
    }
}
