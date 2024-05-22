using UnityEngine;
using UnityEngine.Playables;

namespace com.zibra.common.Timeline
{
    /// <summary>
    ///     Asset representing the  timeline clip.
    /// </summary>
    public class ControlAsset : PlayableAsset
    {
#region Public Interface
        /// <summary>
        ///     What frame rate should the controlled object be targeting.
        /// </summary>
        public float FrameRate = 30.0f;
        internal float ClipStart = 0.0f;
        internal float ClipEnd = 0.0f;
#endregion
#region Implementation details
        /// @cond SHOW_INTERNAL
        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<ControlBehaviour>.Create(graph);

            var vdbControlBehaviour = playable.GetBehaviour();
            vdbControlBehaviour.FrameRate = FrameRate;
            vdbControlBehaviour.FrameOffset = 0;
            vdbControlBehaviour.ClipStart = ClipStart;
            vdbControlBehaviour.ClipEnd = ClipEnd;

            return playable;
        }
        /// @endcond
#endregion
    }
}
