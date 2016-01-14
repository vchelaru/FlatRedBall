using System;
using FlatRedBall;
using FlatRedBall.MSG;
using FlatRedBall.Instructions;


namespace SpriteEditor.seInstructions
{
    /// <summary>
    /// Summary description for CreateSprite.
    /// </summary>
    public class CreateSpriteFrame : FrbInstruction
    {
        public CreateSpriteFrame(SpriteFrame referenceSprite)
        {
            this.referenceObject = referenceSprite;
        }
    }
}
