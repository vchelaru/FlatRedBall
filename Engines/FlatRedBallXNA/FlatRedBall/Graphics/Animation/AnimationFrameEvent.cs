namespace FlatRedBall.Graphics.Animation
{
    /// <summary>
    /// A named event authored on an animation frame (for example "Footstep" or "SummonReady"). A SequenceInstance raises it
    /// when playback enters the frame, so game code reacts by name instead of by time or frame index. A Sprite ignores it.
    /// Written by the FlatRedBall2 AnimationEditor.
    /// </summary>
    public class AnimationFrameEvent
    {
        /// <summary>The event's name. Names need not be unique within a frame.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Optional free-form payload. The engine never reads it. Null when unset.</summary>
        public string Data { get; set; }

        /// <summary>Returns a copy, so a frame copy never shares event instances with its source.</summary>
        public AnimationFrameEvent Clone()
        {
            return new AnimationFrameEvent { Name = Name, Data = Data };
        }
    }
}
