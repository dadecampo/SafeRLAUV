using UnityEngine;

namespace com.zibra.common.Timeline
{
    /// <summary>
    ///     Interface for an object that can be controlled via Timeline.
    /// </summary>
    /// <remarks>
    ///     Intended to be used with <see cref="ZibraControlTrack"/>.
    /// </remarks>
    public abstract class PlaybackControl : MonoBehaviour
    {
#region Public interface
        /// <summary>
        ///     Sets the offset for the playback time of the controlled object.
        /// </summary>
        /// <remarks>
        ///     May not be applicable to certain objects.
        /// </remarks>
        public abstract void SetTime(float time);

        /// <summary>
        ///     Sets the fade weight.
        /// </summary>
        /// <remarks>
        ///     1.0 corresponds to no fade, 0.0 corresponds to full fade.
        /// </remarks>
        public abstract void SetFadeCoefficient(float fadeCoefficient);

        /// <summary>
        ///     Starts playback.
        /// </summary>
        public abstract void StartPlayback(ControlBehaviour controller);

        /// <summary>
        ///     Stops playback.
        /// </summary>
        public abstract void StopPlayback();
#endregion
    }
}
