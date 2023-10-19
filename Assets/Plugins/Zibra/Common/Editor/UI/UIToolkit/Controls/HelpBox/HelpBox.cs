#if UNITY_2019_4_OR_NEWER
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine.UIElements;

namespace com.zibra.liquid.Foundation.UIElements
{
    /// <summary>
    /// The HelpBox component is a UI Toolkit analog for
    /// <see href="https://docs.unity3d.com/ScriptReference/EditorGUILayout.HelpBox.html">EditorGUILayout.HelpBox</see>.
    /// </summary>
    internal class HelpBox : BindableElement
    {
        /// <exclude/>
        [UsedImplicitly]
        internal new class UxmlFactory : UxmlFactory<HelpBox, UxmlTraits>
        {
        }

        /// <exclude/>
        internal new class UxmlTraits : BindableElement.UxmlTraits 
        {
            private readonly UxmlStringAttributeDescription m_Text = new UxmlStringAttributeDescription { name = "text" };
            private readonly UxmlEnumAttributeDescription<MessageType> m_Type =
                new UxmlEnumAttributeDescription<MessageType> { name = "type" };

            public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
            {
                base.Init(ve, bag, cc);
                ((HelpBox)ve).Text = m_Text.GetValueFromBag(bag, cc);
                ((HelpBox)ve).MessageType = m_Type.GetValueFromBag(bag, cc);
            }
        }

        /// <summary>
        /// The message text.
        /// </summary>
        internal string Text { get; set; }

        /// <summary>
        /// The type of message.
        /// </summary>
        internal MessageType MessageType { get; set; }

        /// <summary>
        /// Creates HelpBox control.
        /// </summary>
        public HelpBox()
        {
            Add(new IMGUIContainer(() =>
                                { EditorGUILayout.HelpBox(Text, MessageType, true); }));
        }
    }
}
#endif