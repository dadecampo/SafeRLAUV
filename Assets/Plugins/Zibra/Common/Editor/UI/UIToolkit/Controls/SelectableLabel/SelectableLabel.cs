#if UNITY_2019_4_OR_NEWER
using com.zibra.liquid.Foundation.Editor;
using JetBrains.Annotations;
using UnityEngine.UIElements;

namespace com.zibra.liquid.Foundation.UIElements
{
    /// <summary>
    /// A selectable label field.
    /// The same feature as we use to have with IMGUI
    /// <see href="https://docs.unity3d.com/ScriptReference/EditorGUILayout.SelectableLabel.html"/>
    /// </summary>
    internal class SelectableLabel : Label
    {
        /// <exclude/>
        [UsedImplicitly]
        internal new class UxmlFactory : UxmlFactory<SelectableLabel, UxmlTraits>
        {
        }

        /// <exclude/>
        internal new class UxmlTraits : Label.UxmlTraits {}

        /// <summary>
        /// The text associated with the element.
        /// </summary>
        public override string text 
        { 
            get => TextField.value;
            set => TextField.value = value;
        }

        /// <summary>
        /// SelectableLabel Uss class name.
        /// </summary>
        internal const string USSClassName = "zibraai-selectable-label";

        /// <summary>
        /// SelectableLabel input Uss class name.
        /// </summary>
        internal const string InputUSSClassName = "zibraai-selectable-label__input";

        private TextField m_TextField;

        private TextField TextField
        {
            get {
                if (m_TextField == null)
                {
                    m_TextField = new TextField { isReadOnly = true };
                    m_TextField.Q("unity-text-input").AddToClassList(InputUSSClassName);
                    Add(m_TextField);
                }

                return m_TextField;
            }
        }

        /// <summary>
        /// Creates new settings block.
        /// </summary>
        public SelectableLabel()
        {
            AddToClassList(USSClassName);
            UIToolkitEditorUtility.ApplyStyleForInternalControl(this);
        }
    }
}
#endif