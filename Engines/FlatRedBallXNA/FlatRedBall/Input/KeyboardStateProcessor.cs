using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Input;

namespace FlatRedBall.Input
{
    public class KeyboardStateProcessor
    {
        KeyboardState mLastFrameKeyboardState = new KeyboardState();
        KeyboardState mKeyboardState;

        public bool AnyKeyPushed()
        {
            // loop through all pressed keys...
            for(int i= 0; i < Keyboard.NumberOfKeys; i++)
            {
                // And see if it's pushed (was not down this frame, is down this frame)
                if(KeyPushed((Keys)i))
                {
                    // if so, we can return true
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Clears the keyboard states, simulating the keyboard
        /// not having any values down or pressed
        /// </summary>
        public void Clear()
        {
            mKeyboardState = new KeyboardState();
            mLastFrameKeyboardState = new KeyboardState();
        }

        public bool IsKeyDown(Keys key)
        {
            return mKeyboardState.IsKeyDown(key);
        }

        public bool KeyPushed(Keys key)
        {
            return mKeyboardState.IsKeyDown(key) &&
                !mLastFrameKeyboardState.IsKeyDown(key);
        }

        public bool KeyReleased(Keys key)
        {
            return mLastFrameKeyboardState.IsKeyDown(key) &&
                !mKeyboardState.IsKeyDown(key);
        }

        public void Update(bool useCurrentKeyboardStateAsLast = false)
        {
            if(useCurrentKeyboardStateAsLast)
            {
                // this prevents pushes from happening
                mKeyboardState = Microsoft.Xna.Framework.Input.Keyboard.GetState();
                mLastFrameKeyboardState = mKeyboardState;
            }
            else
            {
                mLastFrameKeyboardState = mKeyboardState;
                mKeyboardState = Microsoft.Xna.Framework.Input.Keyboard.GetState();
            }
        }

    }
}
