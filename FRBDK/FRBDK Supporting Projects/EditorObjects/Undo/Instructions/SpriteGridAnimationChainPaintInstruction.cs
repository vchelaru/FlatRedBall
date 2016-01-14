using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Instructions;
using FlatRedBall.ManagedSpriteGroups;
using FlatRedBall.Graphics.Animation;

namespace EditorObjects.Undo.Instructions
{
    public class SpriteGridAnimationChainPaintInstruction : Instruction
    {
        #region Fields

        SpriteGrid mTarget;
        List<TextureLocation<AnimationChain>> mAnimationChainLocations;

        #endregion

        #region Properties

        public override object Target
        {
            get { return mTarget; }
            set { mTarget = (SpriteGrid)value; }
        }

        #endregion

        #region Methods

        public SpriteGridAnimationChainPaintInstruction(SpriteGrid spriteGrid, List<TextureLocation<AnimationChain>> animationChainLocations)
        {
            mTarget = spriteGrid;
            mAnimationChainLocations = animationChainLocations;
        }

        public override void Execute()
        {
            ExecuteOn(mTarget);
        }

        public override void ExecuteOn(object target)
        {
            SpriteGrid asSpriteGrid = target as SpriteGrid;

            for (int i = 0; i < mAnimationChainLocations.Count; i++)
            {
                TextureLocation<AnimationChain> animationChainLocation = mAnimationChainLocations[i];

                asSpriteGrid.PaintSpriteAnimationChain(
                    animationChainLocation.X,
                    animationChainLocation.Y, 
                    asSpriteGrid.Blueprint.Z,
                    animationChainLocation.Texture);
            }
        }

        #endregion
    }
}
