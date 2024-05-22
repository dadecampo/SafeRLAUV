using UnityEngine;
using UnityEngine.Playables;

namespace com.zibra.common.Timeline
{
    /// <summary>
    ///     Represents behaviour of the timeline clip.
    /// </summary>
    public class ControlBehaviour : PlayableBehaviour
    {
#region Public Interface
        /// <summary>
        ///     What frame rate should controlled object be targeting.
        /// </summary>
        public float FrameRate;

        /// <summary>
        ///     What frame offset should controlled object use.
        /// </summary>
        /// <remarks>
        ///     May not be applicable depending on the controlled object.
        /// </remarks>
        public int FrameOffset;

        /// <summary>
        ///     Time in seconds from the start of timeline when the clip starts.
        /// </summary>
        public float ClipStart;

        /// <summary>
        ///     Time in seconds from the start of timeline when the clip ends.
        /// </summary>
        public float ClipEnd;

        /// <summary>
        ///     Duration of the clip in seconds.
        /// </summary>
        public float Duration { get { return ClipEnd - ClipStart; } }
#endregion
#region Implementation details
        /// @cond SHOW_INTERNAL
        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                return;
            }
#endif

            PlaybackControl playbackControl = playerData as PlaybackControl;
            if (playbackControl != null)
            {
                playbackControl.SetTime((float)(playable.GetTime() + FrameOffset / FrameRate));
                playbackControl.SetFadeCoefficient(info.effectiveWeight);
            }
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                return;
            }
#endif

            PlaybackControl playbackControl = info.output.GetUserData() as PlaybackControl;
            if (playbackControl != null)
            {
                playbackControl.StartPlayback(this);
                playbackControl.SetTime((float)(playable.GetTime() + FrameOffset / FrameRate));
                playbackControl.SetFadeCoefficient(info.effectiveWeight);
            }
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
            {
                return;
            }
#endif

            PlaybackControl playbackControl = info.output.GetUserData() as PlaybackControl;
            if (playbackControl != null)
            {
                playbackControl.StopPlayback();
            }
        }
        /// @endcond
#endregion
    }
}
