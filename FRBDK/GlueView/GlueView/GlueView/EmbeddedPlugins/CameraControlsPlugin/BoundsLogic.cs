using FlatRedBall.Math.Geometry;
using GlueView.Facades;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlueView.EmbeddedPlugins.CameraControlsPlugin
{
    class BoundsLogic
    {
        AxisAlignedRectangle boundsRectangle;

        internal void HandleElementLoaded()
        {
            if(GlueViewState.Self.CurrentGlueProject != null)
            {
                if(boundsRectangle == null)
                {
                    boundsRectangle = ShapeManager.AddAxisAlignedRectangle();
                }

                boundsRectangle.Width = GlueViewState.Self.CurrentGlueProject.OrthogonalWidth;
                boundsRectangle.Height = GlueViewState.Self.CurrentGlueProject.OrthogonalHeight;
            }
        }
    }
}
