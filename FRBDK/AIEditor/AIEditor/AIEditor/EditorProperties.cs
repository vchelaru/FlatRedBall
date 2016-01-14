using System;

using FlatRedBall;
using FlatRedBall.ManagedSpriteGroups;
#if FRB_MDX
using System.Drawing;
#else
using Color = Microsoft.Xna.Framework.Graphics.Color;
#endif
namespace AIEditor
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

        public Color LinkColor
        {
            get { return EditorData.NodeNetwork.LinkColor; }
            set { EditorData.NodeNetwork.LinkColor = value; }
        }

        public Color NodeColor
        {
            get { return EditorData.NodeNetwork.NodeColor; }
            set { EditorData.NodeNetwork.NodeColor = value; }
        }

        public bool Filtering
        {
            get
            {
                return FlatRedBallServices.GraphicsOptions.TextureFilter != Microsoft.Xna.Framework.Graphics.TextureFilter.Point;
            }
            set
            {
                if (value)
                {
                    FlatRedBallServices.GraphicsOptions.TextureFilter = Microsoft.Xna.Framework.Graphics.TextureFilter.Linear;
                }
                else
                {
                    FlatRedBallServices.GraphicsOptions.TextureFilter = Microsoft.Xna.Framework.Graphics.TextureFilter.Point;
                }
            }
        }

        #endregion

        #region Methods
        public EditorProperties()
        {
        }
        #endregion
    }
}
