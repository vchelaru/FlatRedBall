using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework.Graphics;

using FlatRedBall;

using FlatRedBall.Graphics;
using FlatRedBall.Graphics.Model;

using FlatRedBall.Math.Collision;

using FlatRedBall.Math;
using FlatRedBall.Graphics.Particle;
using System.Reflection;

namespace XmlObjectEditor
{
    public class EditorLogic
    {
        Assembly mCurrentAssembly;

        public Assembly CurrentAssembly
        {
            get { return mCurrentAssembly; }
            set { mCurrentAssembly = value; }
        }
    }
}
