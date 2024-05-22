using UnityEngine.Timeline;

namespace com.zibra.common.Timeline
{
    /// <summary>
    ///     Timeline track for controlling objects of type <see cref="PlaybackControl"/>.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Object to control is specified via the Track Binding.
    ///     </para>
    ///     <para>
    ///         Used with <see cref="ControlAsset"/> that specifies time bounds and .
    ///     </para>
    /// </remarks>
    [TrackClipType(typeof(ControlAsset))]
    [TrackBindingType(typeof(PlaybackControl))]
    public class ZibraControlTrack : TrackAsset
    {
#region Implementation details
        /// @cond SHOW_INTERNAL
        private void OnEnable()
        {
            foreach (var clip in GetClips())
            {
                ControlAsset controlAsset = clip.asset as ControlAsset;
                if (controlAsset != null)
                {
                    controlAsset.ClipStart = (float)clip.start;
                    controlAsset.ClipEnd = (float)clip.end;
                }
            }
        }
        /// @endcond
#endregion
    }
}
