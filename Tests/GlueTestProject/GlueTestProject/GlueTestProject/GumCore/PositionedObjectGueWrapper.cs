using FlatRedBall;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GumCoreShared.FlatRedBall.Embedded
{
    public class PositionedObjectGueWrapper : PositionedObject
    {
        public PositionedObject FrbObject { get; set; }

        GraphicalUiElement GumParent { get; set; }
        public GraphicalUiElement GumObject { get; set; }

        public PositionedObjectGueWrapper(PositionedObject frbObject, GraphicalUiElement gumObject) : base()
        {

            var renderable = new InvisibleRenderable();
            renderable.Visible = true;

            GumParent = new GraphicalUiElement();
            GumParent.SetContainedObject(renderable);
            GumParent.XUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
            GumParent.YUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;

            

            this.FrbObject = frbObject;
            this.GumObject = gumObject;
            
            gumObject.Parent = GumParent;
        }

        public override void UpdateDependencies(double currentTime)
        {
            base.UpdateDependencies(currentTime);

            // This is going to get positioned according to the FRB object. I guess we'll force update dependencies, which is expensive...
            FrbObject.UpdateDependencies(currentTime);

            // todo - need to support multiple cameras and layers
            var camera = Camera.Main;

            int screenX = 0;
            int screenY = 0;

            global::FlatRedBall.Math.MathFunctions.AbsoluteToWindow(
                FrbObject.X, FrbObject.Y, FrbObject.Z,
                ref screenX, ref screenY, camera);

            GumParent.X = screenX;
            GumParent.Y = screenY;
        }
    }
}
