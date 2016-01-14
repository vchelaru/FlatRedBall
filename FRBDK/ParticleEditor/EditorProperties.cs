using System;

using FlatRedBall;
using FlatRedBall.ManagedSpriteGroups;

#if FRB_MDX
using System.Drawing;
#else
using Color = Microsoft.Xna.Framework.Graphics.Color;
#endif
using System.Collections.Generic;

namespace ParticleEditor
{
    public class EditorProperties
    {
        #region Fields
        Color mBackgrounColor;

        #endregion

        #region Properties
 
        public Color BackgroundColor
        {
            get { return SpriteManager.Camera.BackgroundColor; }
            set { SpriteManager.Camera.BackgroundColor = value; }
        }

        public List<string> FilesToMakeDotDotRelative
        {
            get;
            set;
        }

        #endregion

        #region Methods

        public EditorProperties()
        {
            FilesToMakeDotDotRelative = new List<string>();
        }
        #endregion
    }
}
