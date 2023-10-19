#if UNITY_2019_4_OR_NEWER
using com.zibra.liquid.Foundation.Editor;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UIElements;

namespace com.zibra.liquid.Foundation.UIElements
{
    /// <summary>
    /// The LoadingSpinner control. let's you place buttons group with the labels or images
    /// </summary>
    internal sealed class LoadingSpinner : VisualElement
    {
        [UsedImplicitly]
        internal new class UxmlFactory : UxmlFactory<LoadingSpinner, UxmlTraits>
        {
        }

        internal new class UxmlTraits : BindableElement.UxmlTraits {}

        private bool m_IsActive;
        private int m_RotationAngle;

        private readonly IVisualElementScheduledItem m_ScheduledUpdate;

        private const long k_RotationUpdateInterval = 1L;
        private const int k_RotationAngleDelta = 10;

        /// <summary>
        /// Loading Spinner control Uss class name
        /// </summary>
        internal const string UssClassName = "zibraai-loading-spinner";

        /// <summary>
        /// Creates LoadingSpinner control
        /// </summary>
        public LoadingSpinner()
        {
            AddToClassList(UssClassName);
            UIToolkitEditorUtility.ApplyStyleForInternalControl(this, nameof(LoadingSpinner));
            m_IsActive = false;

            // add child elements to set up centered spinner rotation
            var innerElement = new VisualElement();
            innerElement.AddToClassList("image");
            Add(innerElement);

            m_ScheduledUpdate = schedule.Execute(UpdateProgress).Every(k_RotationUpdateInterval);
            m_ScheduledUpdate.Pause();

            RegisterCallback<AttachToPanelEvent>(OnAttachToPanelEventHandler, TrickleDown.TrickleDown);
            RegisterCallback<DetachFromPanelEvent>(OnDetachFromPanelEventHandler, TrickleDown.TrickleDown);
        }

        private void OnAttachToPanelEventHandler(AttachToPanelEvent e)
        {
            Activate();
        }

        private void OnDetachFromPanelEventHandler(DetachFromPanelEvent e)
        {
            Deactivate();
        }

        private void UpdateProgress()
        {
            transform.rotation = Quaternion.Euler(0, 0, m_RotationAngle);
            m_RotationAngle += k_RotationAngleDelta;
            if (m_RotationAngle > 360)
                m_RotationAngle -= 360;
        }

        private void Activate()
        {
            if (m_IsActive)
                return;

            m_RotationAngle = 0;
            m_ScheduledUpdate.Resume();

            m_IsActive = true;
        }

        private void Deactivate()
        {
            if (!m_IsActive)
                return;

            m_ScheduledUpdate.Pause();

            m_IsActive = false;
        }
    }
}
#endif
