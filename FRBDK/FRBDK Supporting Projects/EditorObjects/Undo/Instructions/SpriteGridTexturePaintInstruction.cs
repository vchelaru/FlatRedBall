using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Instructions;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall;
#if FRB_XNA
using Microsoft.Xna.Framework.Graphics;
#endif

namespace EditorObjects.Undo.Instructions
{
    public class SpriteGridTexturePaintInstruction : Instruction
    {
        #region Fields

        SpriteGrid mTarget;
        List<TextureLocation<Texture2D>> mTextureLocations;

        #endregion

        #region Properties

        public override object Target
        {
            get { return mTarget; }
            set { mTarget = (SpriteGrid)value; }
        }

        #endregion

        #region Methods

        public SpriteGridTexturePaintInstruction(SpriteGrid spriteGrid, List<TextureLocation<Texture2D>> textureLocations)
        {
            mTarget = spriteGrid;
            mTextureLocations = textureLocations;
        }

        public override void Execute()
        {
            ExecuteOn(mTarget);
        }

        public override void ExecuteOn(object target)
        {
            SpriteGrid asSpriteGrid = target as SpriteGrid;


            for (int i = 0; i < mTextureLocations.Count; i++)
            {
                TextureLocation<Texture2D> textureLocation = mTextureLocations[i];

                asSpriteGrid.PaintSprite(textureLocation.X, textureLocation.Y, asSpriteGrid.Blueprint.Z, textureLocation.Texture);
            }
        }

        #endregion
    }
}
