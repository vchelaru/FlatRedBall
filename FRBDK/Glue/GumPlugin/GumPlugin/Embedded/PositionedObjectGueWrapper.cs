using FlatRedBall;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GumCoreShared.FlatRedBall.Embedded
{
    public class PositionedObjectGueWrapper : GraphicalUiElement
    {
        public PositionedObject FrbObject { get; set; }
        public GraphicalUiElement GumObject { get; set; }

        public PositionedObjectGueWrapper(PositionedObject frbObject, GraphicalUiElement gumObject) : base()
        {
            var renderable = new InvisibleRenderable();
            renderable.Visible = true;
            this.SetContainedObject(renderable);
            this.XUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
            this.YUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;

            

            this.FrbObject = frbObject;
            this.GumObject = gumObject;

            gumObject.Parent = this;
        }

        public void UpdateGumPosition()
        {
            // This is going to get positioned according to the FRB object. I guess we'll force update dependencies, which is expensive...
            FrbObject.ForceUpdateDependencies();

            // todo - need to support multiple cameras and layers
            var camera = Camera.Main;

            int screenX = 0;
            int screenY = 0;

            global::FlatRedBall.Math.MathFunctions.AbsoluteToWindow(
                FrbObject.X, FrbObject.Y, FrbObject.Z,
                ref screenX, ref screenY, camera);

            this.X = screenX;
            this.Y = screenY;
        }
    }
}
