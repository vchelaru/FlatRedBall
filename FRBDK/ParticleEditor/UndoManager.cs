using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;

using FlatRedBall.Input;

using FlatRedBall.Instructions;
#if FRB_MDX

#else
using Keys = Microsoft.Xna.Framework.Input.Keys;
#endif
namespace ParticleEditor
{
    public class UndoManager
    {
        static List<InstructionList> mInstructions = new List<InstructionList>();

        #region Properties

        public static List<InstructionList> Instructions
        {
            get { return mInstructions; }
        }

        #endregion

        public static void EndOfFrameActivity()
        {
            if (InputManager.Keyboard.KeyDown(Keys.LeftControl) &&
                InputManager.Keyboard.KeyPushed(Keys.Z) &&
                mInstructions.Count != 0)
            {
                InstructionList instructionList = mInstructions[mInstructions.Count - 1];

                instructionList.Execute();
                mInstructions.RemoveAt(mInstructions.Count - 1);

            }
        }
    }
}
