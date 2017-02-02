using System;
using System.Collections.Generic;

namespace FlatRedBall.Input
{
	public interface IPressableInput
	{
        /// <summary>
        /// Returns whether the input is down (such as if a key is held or a mouse button is down)
        /// </summary>
		bool IsDown { get; }
        /// <summary>
        /// Returns whether the input was pressed this frame (not down last frame, is down this frame)
        /// </summary>
		bool WasJustPressed { get; }
		bool WasJustReleased { get; }
	}

    public class DelegateBasedPressableInput : IPressableInput
    {
        Func<bool> isDown;
        Func<bool> wasJustPressed;
        Func<bool> wasJustReleased;

        public DelegateBasedPressableInput(Func<bool> isDown, Func<bool> wasJustPressed, Func<bool> wasJustReleased)
        {
            this.isDown = isDown;
            this.wasJustPressed = wasJustPressed;
            this.wasJustReleased = wasJustReleased;
        }

        public bool IsDown
        {
            get { return this.isDown(); }
        }

        public bool WasJustPressed
        {
            get { return this.wasJustPressed(); }
        }

        public bool WasJustReleased
        {
            get { return this.wasJustReleased(); }
        }
    }

    /// <summary>
    /// An IPressableInput interface which can contain multiple IPressableInputs. This is useful if a particular action can be
    /// performed with multiple inputs, such as both the space bar and a game pad's A button being used to make a character jump.
    /// </summary>
    public class MultiplePressableInputs : IPressableInput
    {
        /// <summary>
        /// The list of inputs to be used for an action.
        /// </summary>
        /// <example>
        /// // The following shows how to add the space bar and the enter key:
        /// var jumpInput = new MultiplePressableInputs();
        /// jumpInput.Inputs.Add(InputManager.Keyboard.GetKey(Keys.Space));
        /// jumpInput.Inputs.Add(InputManager.Keyboard.GetKey(Keys.Enter));
        /// </example>
        public List<IPressableInput> Inputs
        {
            get;
            private set;
        }

        public MultiplePressableInputs()
        {
            Inputs = new List<IPressableInput>();
        }



        public bool IsDown
        {
            get 
            {
                foreach (var input in Inputs)
                {
                    if(input.IsDown)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public bool WasJustPressed
        {
            get
            {
                foreach (var input in Inputs)
                {
                    if (input.WasJustPressed)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public bool WasJustReleased
        {
            get
            {
                foreach (var input in Inputs)
                {
                    if (input.WasJustReleased)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
    }
}

