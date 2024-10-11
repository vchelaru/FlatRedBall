using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Audio;

namespace FlatRedBall.Audio
{
    // This is a struct so we don't
    // trigger the GC a lot by using
    // these.  I don't think we ever need 
    // to reference these.
    public struct SoundEffectPlayInfo
    {
        public SoundEffect SoundEffect;
        public double LastPlayTime;

    }

    //public struct SoundEffectInstancePlayInfo
    //{
    //    public SoundEffectInstance SoundEffect;
    //    public double LastPlayTime;

    //}

}
