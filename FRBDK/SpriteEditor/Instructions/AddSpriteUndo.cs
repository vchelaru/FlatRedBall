using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Instructions;
using FlatRedBall;

namespace SpriteEditor.Instructions
{
    public class AddSpriteUndo : Instruction
    {
        #region Fields

        private Sprite mTarget;

        #endregion

        #region Properties

        public override object Target
        {
            get { return mTarget; }
            set { mTarget = (Sprite)value; }
        }

        #endregion

        #region Methods

        public AddSpriteUndo(Sprite spriteAdded)
        {
            mTarget = spriteAdded;
        }

        public override void Execute()
        {
            ExecuteOn(mTarget);
        }


        public override void ExecuteOn(object target)
        {
            SpriteManager.RemoveSprite(mTarget);
        }

        #endregion
    }
}
