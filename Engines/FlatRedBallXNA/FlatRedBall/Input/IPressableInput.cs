using System;
using System.Collections.Generic;

namespace FlatRedBall.Input
{
    /// <summary>
    /// Provides a common interface for input devices which can have a down/not down state, such as a game pad or mouse button.
    /// </summary>
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

        /// <summary>
        /// Returns whether the input was released this frame(down last frame, not down this frame)
        /// </summary>
        bool WasJustReleased { get; }
	}


    public interface IRepeatPressableInput : IPressableInput
    {
        /// <summary>
        /// Returns whether the input was pressed this frame (not down last frame, is down this frame), or
        /// whether a repeat push occurred from the input being held down.
        /// </summary>
        bool WasJustPressedOrRepeated { get; }
    }

    /// <summary>
    /// Implementation of IPressableInput which always returns false. Can be used for classes
    /// requiring an IPressableInput implementation
    /// (like IInputDevice) which sould always return false.
    /// </summary>
    public class FalsePressableInput : IRepeatPressableInput
    {
        public static FalsePressableInput Instance = new FalsePressableInput();
        public bool IsDown => false;

        public bool WasJustPressed => false;

        public bool WasJustReleased => false;

        public bool WasJustPressedOrRepeated => false;
    }

    public class DelegateBasedPressableInput : IRepeatPressableInput
    {
        Func<bool> isDown;
        Func<bool> wasJustPressed;
        Func<bool> wasJustReleased;
        Func<bool> wasJustPressedOrRepeated;

        public DelegateBasedPressableInput(Func<bool> isDown, Func<bool> wasJustPressed, Func<bool> wasJustReleased,
            Func<bool> wasJustPressedOrRepeated = null)
        {
            this.isDown = isDown;
            this.wasJustPressed = wasJustPressed;
            this.wasJustReleased = wasJustReleased;
            this.wasJustPressedOrRepeated = wasJustPressedOrRepeated;
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

        public bool WasJustPressedOrRepeated
        {
            get {  return wasJustPressedOrRepeated?.Invoke() ?? false; }
        }
    }

    /// <summary>
    /// Class including extension methods on the IPressableInput interface.
    /// </summary>
    public static class IPressableInputExtensions
    {
        /// <summary>
        /// Allows making a single PressableInput (of type MultiplePressableInputs) for inputs that combine more than one IPressableInput instance.
        /// </summary>
        /// <param name="thisInput">The calling IPressableInput</param>
        /// <param name="input">The second IPressableInput to add when creating a MultiplePressableInputs</param>
        /// <returns>The resulting IPressableInput which contains the caller and the argument input.</returns>
        public static MultiplePressableInputs Or(this IPressableInput thisInput, IPressableInput input)
        {
            MultiplePressableInputs toReturn;
            if(thisInput is MultiplePressableInputs)
            {
                toReturn = (MultiplePressableInputs)thisInput;
            }
            else
            {
                toReturn = new MultiplePressableInputs();
                toReturn.Inputs.Add(thisInput);

                if(thisInput is IRepeatPressableInput thisAsRepeatable)
                {
                    toReturn.RepeatPressableInputs.Add(thisAsRepeatable);
                }
            }

            toReturn.Inputs.Add(input);
            if(input is IRepeatPressableInput inputAsRepeatable)
            {
                toReturn.Inputs.Add(inputAsRepeatable);
            }

            return toReturn;
        }


        /// <summary>
        /// Creates a new I2DInput from the calling IPressableInput which returns a Value of 0 if not pressed, and 1 if pressed.
        /// </summary>
        /// <param name="thisInput">The IpressableInput to use as a 1DInput</param>
        /// <returns>The resulting I1DInput.</returns>
        public static I1DInput To1DInput(this IPressableInput thisInput)
        {
            var toReturn = new DelegateBased1DInput(
                () => thisInput.IsDown ? 1 : 0,
                () => 0
                );

            return toReturn;
        }
    }

    /// <summary>
    /// An IPressableInput interface which can contain multiple IPressableInputs. This is useful if a particular action can be
    /// performed with multiple inputs, such as both the space bar and a game pad's A button being used to make a character jump.
    /// </summary>
    public class MultiplePressableInputs : IRepeatPressableInput
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

        public List<IRepeatPressableInput> RepeatPressableInputs
        {
            get;
            private set;
        }

        public MultiplePressableInputs()
        {
            Inputs = new List<IPressableInput>();
            RepeatPressableInputs = new List<IRepeatPressableInput>();
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

        public bool WasJustPressedOrRepeated
        {
            get
            {
                if(WasJustPressed)
                {
                    return true;
                }
                foreach(var repeatPressableInput in RepeatPressableInputs)
                {
                    if(repeatPressableInput.WasJustPressedOrRepeated)
                    {
                        if(repeatPressableInput.WasJustPressedOrRepeated)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }
    }
}

