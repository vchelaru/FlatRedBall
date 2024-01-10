using System;
#if FRB_MDX
using Microsoft.DirectX.DirectInput;

using Keys = Microsoft.DirectX.DirectInput.Key;
#else
using Microsoft.Xna.Framework.Input;
#endif

namespace FlatRedBall.Input
{
	public class KeyReference : IRepeatPressableInput
	{
		public Keys Key{ get; set;}

        // Originally this was an explicit implementation, but not sure why:
		public bool IsDown
		{
			get 
			{
				return InputManager.Keyboard.KeyDown (Key);
			}
		}
		public bool WasJustPressed 
		{
			get 
			{
				return InputManager.Keyboard.KeyPushed (Key);
			}
		}
		public bool WasJustReleased 
		{
			get 
			{
				return InputManager.Keyboard.KeyReleased (Key);
			}
		}

		public bool WasJustPressedOrRepeated => InputManager.Keyboard.KeyTyped(Key);
    }
}

