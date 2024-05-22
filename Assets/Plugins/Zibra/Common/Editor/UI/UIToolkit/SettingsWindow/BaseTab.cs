using com.zibra.common.Foundation.Editor;
using UnityEngine.UIElements;

namespace com.zibra.common.Plugins.Editor
{
    /// <summary>
    /// Base window tab implementation for <see cref="PackageSettingsWindow{TWindow}" />
    /// </summary>
    internal abstract class BaseTab : VisualElement
    {
        /// <summary>
        /// Created tab with the content of provided uxml file.
        /// </summary>
        /// <param name="path">Project related uxml/uss file path without extensions.</param>
        protected BaseTab(string path)
        {
            UIToolkitEditorUtility.CloneTreeAndApplyStyle(this, path);
        }

        /// <summary>
        /// Tab root.
        /// </summary>
        public VisualElement Root => this;
    }
}
