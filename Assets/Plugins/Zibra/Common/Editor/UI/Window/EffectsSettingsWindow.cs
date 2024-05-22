using com.zibra.common.Plugins.Editor;
using UnityEngine;
using UnityEngine.UIElements;
using com.zibra.common.Editor.SDFObjects;
using com.zibra.common.Editor;

namespace com.zibra.common
{
    internal class EffectsSettingsWindow : PackageSettingsWindow<EffectsSettingsWindow>
    {
        internal override IPackageInfo GetPackageInfo() => new ZibraAiPackageInfo();

        protected override void OnWindowEnable(VisualElement root)
        {
            AddTab("Info", new AboutTab());
        }

        internal static GUIContent WindowTitle => new GUIContent(ZibraAIPackage.DisplayName);
    }
}
