using FlatRedBallAndroid.Input;
using Microsoft.Xna.Framework.Input;
using System;

namespace FlatRedBall.Input
{
	public static partial class InputManager
	{
        static partial void PlatformSpecificXbox360GamePadUpdate()
        {
            for(int i = 0; i < 4; i++)
            {
                var state = AndroidGamePadManager.GetState(i, GamePadDeadZone.Circular);

                mXbox360GamePads[i].Update(state);
            }

        }

    }
}

