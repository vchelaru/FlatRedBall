using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework;

namespace FlatRedBall.Audio
{
    // VIC:  Why do we have this?  Figure this out.
    public class PositionedSoundListener : PositionedObject
    {
        #region Fields

        internal AudioListener Listener;

        #endregion

        #region Constructor

        public PositionedSoundListener()
            : base()
        {
            Listener = new AudioListener();
        }

        #endregion

        #region Internal Methods

        internal void UpdateAudio()
        {
            Listener.Position = this.Position;
            Listener.Forward = Vector3.Transform(Vector3.Forward, this.RotationMatrix);
            Listener.Up = Vector3.Transform(Vector3.Up, this.RotationMatrix);
            Listener.Velocity = this.Velocity;
        }

        #endregion
    }
}
