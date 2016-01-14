using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Math;
using FlatRedBall.Graphics.Particle;

namespace ParticleEditor
{
    public class AppState : Singleton<AppState>
    {

        public string PermanentContentManager = "Permenant Content Manager";
        private Emitter mCurrentEmitter;

        public PositionedObjectList<Emitter> Emitters
        {
            get
            {
                return EditorData.Emitters;
            }
            set
            {
                EditorData.Emitters = value;
            }
        }

        public Emitter CurrentEmitter
        {
            get { return mCurrentEmitter; }
            set
            {
                mCurrentEmitter = value;
            }
        }
    }
}
