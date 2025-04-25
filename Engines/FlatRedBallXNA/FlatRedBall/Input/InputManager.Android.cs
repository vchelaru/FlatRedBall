using FlatRedBallAndroid.Input;
using Microsoft.Xna.Framework.Input;
using System;

namespace FlatRedBall.Input
{
	public static partial class InputManager
	{
        static partial void PlatformSpecificXbox360GamePadUpdate()
        {
            // This used to be needed in the old days, but it's not needed anymore as of MonoGame 3.8.1
            // Keeping it here as reference in case we ever want to work directly with Android input:
            //for(int i = 0; i < 4; i++)
            //{
            //    var state = AndroidGamePadManager.GetState(i, GamePadDeadZone.Circular);

            //    mXbox360GamePads[i].Update(state);
            //}

        }

    }
}

