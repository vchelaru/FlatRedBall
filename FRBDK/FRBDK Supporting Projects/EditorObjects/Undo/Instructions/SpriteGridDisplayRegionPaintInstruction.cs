using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Instructions;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Math.Geometry;

namespace EditorObjects.Undo.Instructions
{
    public class SpriteGridDisplayRegionPaintInstruction : Instruction
    {
        #region Fields

        SpriteGrid mTarget;
        List<TextureLocation<FloatRectangle>> mDisplayRegions;

        #endregion

        #region Properties

        public override object Target
        {
            get { return mTarget; }
            set { mTarget = (SpriteGrid)value; }
        }

        #endregion

        #region Methods

        public SpriteGridDisplayRegionPaintInstruction(SpriteGrid spriteGrid, List<TextureLocation<FloatRectangle>> displayRegions)
        {
            mTarget = spriteGrid;
            mDisplayRegions = displayRegions;
        }

        public override void Execute()
        {
            ExecuteOn(mTarget);
        }

        public override void ExecuteOn(object target)
        {
            SpriteGrid asSpriteGrid = target as SpriteGrid;

            for (int i = 0; i < mDisplayRegions.Count; i++)
            {
                TextureLocation<FloatRectangle> displayRegion = mDisplayRegions[i];

                FloatRectangle rectangle = displayRegion.Texture;

                asSpriteGrid.PaintSpriteDisplayRegion(
                    displayRegion.X, displayRegion.Y, asSpriteGrid.Blueprint.Z, ref rectangle);
            }
        }

        #endregion
    }
}
