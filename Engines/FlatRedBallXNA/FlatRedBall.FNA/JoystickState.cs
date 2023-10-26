namespace Microsoft.Xna.Framework.Input;

public class JoystickHat {
	public ButtonState Left { get; set; }

	public ButtonState Right { get; set; }

	public ButtonState Down { get; set; }

	public ButtonState Up { get; set; }
}

public class JoystickState {
	public bool IsConnected { get; private set; }

	public JoystickHat[] Hats { get; set; }

	public float[] Axes { get; set; }

	public ButtonState[] Buttons { get; set; }
}