using FlatRedBall.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Audio
{
    public interface ISong : INameable, IDisposable
    {
        bool IsPlaying { get; }
        bool IsRepeating { get; set; }
        float Volume { get; set; }
        event EventHandler PlaybackStopped;
        void Play();
        void Stop();
    }
}
