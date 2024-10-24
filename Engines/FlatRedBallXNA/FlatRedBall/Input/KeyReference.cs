using System;
using Microsoft.Xna.Framework.Input;

namespace FlatRedBall.Input
{
	/// <summary>
	/// An IRepeatPressableInput implementation that references a key on the Keyboard
	/// </summary>
	public class KeyReference : IRepeatPressableInput
	{
		/// <summary>
		/// The Key to reference on Keyboard
		/// </summary>
		public Keys Key{ get; set;}

		/// <summary>
		/// Whether the key is currently being held down
		/// </summary>
		public bool IsDown => InputManager.Keyboard.KeyDown (Key);

		/// <summary>
		/// Whether the Key was just pressed (not down last frame, is down this frame)
		/// </summary>
		public bool WasJustPressed  => InputManager.Keyboard.KeyPushed (Key);

		/// <summary>
		/// Whether the Key was just released (down last frame, not down this frame)
		/// </summary>
		public bool WasJustReleased  => InputManager.Keyboard.KeyReleased (Key);

		/// <summary>
		/// Whether the Key was just pressed or is being held down
		/// </summary>
		public bool WasJustPressedOrRepeated => InputManager.Keyboard.KeyTyped(Key);

		/// <summary>
		/// Returns the string representation of the KeyReference
		/// </summary>
		/// <returns>The string representation of the KeyReference</returns>
        public override string ToString() => $"KeyReference: {Key}";
    }
}

