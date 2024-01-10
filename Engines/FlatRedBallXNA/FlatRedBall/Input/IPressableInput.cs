using System;
using System.Collections.Generic;
using System.Linq;

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
    /// (like IInputDevice) which could always return false.
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
        /// Returns an IPressableInput which requires both of the provided inputs to be true for its inputs to be true.
        ///   In other words: AndPressableInput.WasJustPressed is equivalent to (input1.WasJustPressed &amp;&amp; input2.WasJustPressed)
        ///   and so on.
        /// </summary>
        public static AndPressableInput And(this IPressableInput thisInput, IPressableInput otherInput)
        {
            if (thisInput is AndPressableInput andInput1)
            {
                andInput1.AddInput(otherInput);
                return andInput1;
            }
            
            if (otherInput is AndPressableInput andInput2)
            {
                andInput2.AddInput(otherInput);
                return andInput2;
            }
            
            return new AndPressableInput(thisInput, otherInput);
        }
        
        /// <summary>
        /// Returns an IPressableInput which requires any of the provided inputs to be true for its inputs to be true.
        ///   In other words: OrPressableInput.WasJustPressed is equivalent to (input1.WasJustPressed || input2.WasJustPressed)
        ///   and so on.
        /// </summary>
        public static OrPressableInput Or(this IPressableInput thisInput, IPressableInput otherInput)
        {
            if (thisInput is OrPressableInput orInput1)
            {
                orInput1.AddInput(otherInput);
                return orInput1;
            }
            
            if (otherInput is OrPressableInput orInput2)
            {
                orInput2.AddInput(otherInput);
                return orInput2;
            }
            
            return new OrPressableInput(thisInput, otherInput);
        }
        
        /// <summary>
        /// Returns an IPressableInput which requires the first of the provided inputs to be true
        ///   and the second of the provided inputs to be false for its inputs to be true.
        ///   In other words: AndNotPressableInput.WasJustPressed is equivalent to (input1.WasJustPressed &amp;&amp; !input2.WasJustPressed)
        ///   and so on.
        /// <br/>Does not support checking for repeat inputs.
        /// </summary>
        public static AndNotPressableInput AndNot(this IPressableInput thisInput, IPressableInput otherInput)
        {
            return new AndNotPressableInput(thisInput, otherInput);
        }

        /// <summary>
        /// Creates a new I2DInput from the calling IPressableInput which returns a Value of 0 if not pressed, and 1 if pressed.
        /// </summary>
        /// <param name="thisInput">The IPressableInput to use as a 1DInput</param>
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
    public abstract class MultiPressableInputBase
    {
        protected MultiPressableInputBase(IPressableInput input1, IPressableInput input2)
        {
            AddInput(input1);
            AddInput(input2);
        }
        
        protected MultiPressableInputBase(IEnumerable<IPressableInput> otherInputs)
        {
            if (!otherInputs.Any())
            {
                throw new ArgumentException("MultiInputs must contain at least one input.");
            }
            foreach (IPressableInput pressableInput in otherInputs)
            {
                AddInput(pressableInput);
            }
        }

        /// <summary>
        /// Use to add another input to this existing MultiInput. Automatically detects <see cref="IRepeatPressableInput"/>s
        ///   and adds them accordingly.
        /// </summary>
        public void AddInput(IPressableInput input)
        {
            if (input is IRepeatPressableInput repeatInput)
            {
                RepeatableInputs.Add(repeatInput);
            }
            Inputs.Add(input);
        }

        protected PressableInputCollection<IPressableInput> Inputs { get; } = new PressableInputCollection<IPressableInput>();
        protected RepeatPressableInputCollection RepeatableInputs { get; } = new RepeatPressableInputCollection();
    }

    /// <summary>
    /// <inheritdoc cref="MultiPressableInputBase"/>
    /// <br/>This input requires all contained inputs to be true for it to be true.
    /// </summary>
    public class AndPressableInput : MultiPressableInputBase, IRepeatPressableInput
    {
        public AndPressableInput(IPressableInput input1, IPressableInput input2) : base(input1, input2) { }
        public AndPressableInput(IEnumerable<IPressableInput> otherInputs) : base(otherInputs) { }
        
        public bool IsDown => Inputs.AllDown;
        public bool WasJustPressed => Inputs.AllJustPressed;
        public bool WasJustReleased => Inputs.AllJustReleased;
        public bool WasJustPressedOrRepeated => WasJustPressed || RepeatableInputs.AllPressedOrRepeated;
    }


    /// <summary>
    /// <inheritdoc cref="MultiPressableInputBase"/>
    /// <br/>This input requires at least one of the contained inputs to be true for it to be true.
    /// </summary>
    public class OrPressableInput : MultiPressableInputBase, IRepeatPressableInput
    {
        public OrPressableInput(IPressableInput input1, IPressableInput input2) : base(input1, input2) { }
        public OrPressableInput(IEnumerable<IPressableInput> otherInputs) : base(otherInputs) { }

        public bool IsDown => Inputs.AnyDown;
        public bool WasJustPressed => Inputs.AnyJustPressed;
        public bool WasJustReleased => Inputs.AnyJustReleased;
        public bool WasJustPressedOrRepeated => WasJustPressed || RepeatableInputs.AnyJustPressedOrRepeated;
    }

    /// <summary>
    /// Holds two pressable inputs. Inputs return true if and only if the first input is true and the second input is false.
    /// </summary>
    public class AndNotPressableInput : IPressableInput
    {
        public AndNotPressableInput(IPressableInput input, IPressableInput notInput)
        {
            Input = input;
            NotInput = notInput;
        }
        
        protected IPressableInput Input { get; }
        protected IPressableInput NotInput { get; }

        public bool IsDown => Input.IsDown & !NotInput.IsDown;
        public bool WasJustPressed => Input.WasJustPressed & !NotInput.WasJustPressed;
        public bool WasJustReleased => Input.WasJustReleased & !NotInput.WasJustReleased;
    }

    /// <summary>
    /// Stores pressable inputs and does basic queries to see if any or all of the inputs are active.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PressableInputCollection<T> : HashSet<T> where T : IPressableInput
    {
        public bool AllJustPressed
        {
            get
            {
                if (Count == 0) throw new InvalidOperationException();
                
                foreach (var input in this)
                {
                    if (!input.WasJustPressed)
                    {
                        return false;
                    }
                }
                return true;
            }
        }
        
        public bool AllJustReleased
        {
            get
            {
                if (Count == 0) throw new InvalidOperationException();
                
                foreach (var input in this)
                {
                    if (!input.WasJustReleased)
                    {
                        return false;
                    }
                }
                return true;
            }
        }
        
        public bool AllDown
        {
            get
            {
                if (Count == 0) throw new InvalidOperationException();
                
                foreach (var input in this)
                {
                    if (!input.IsDown)
                    {
                        return false;
                    }
                }
                return true;
            }
        }
        
        public bool AnyJustPressed
        {
            get
            {
                if (Count == 0) throw new InvalidOperationException();
                
                foreach (var input in this)
                {
                    if (input.WasJustPressed)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
        
        public bool AnyJustReleased
        {
            get
            {
                if (Count == 0) throw new InvalidOperationException();
                
                foreach (var input in this)
                {
                    if (input.WasJustReleased)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
        
        public bool AnyDown
        {
            get
            {
                if (Count == 0) throw new InvalidOperationException();
                
                foreach (var input in this)
                {
                    if (input.IsDown)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
    }

    public class RepeatPressableInputCollection : PressableInputCollection<IRepeatPressableInput>
    {
        public bool AllPressedOrRepeated
        {
            get
            {
                if (Count == 0) throw new InvalidOperationException();
                
                foreach(var repeatPressableInput in this)
                {
                    if(!repeatPressableInput.WasJustPressedOrRepeated)
                    {
                        return false;
                    }
                }
                return true;
            }
        }
        
        public bool AnyJustPressedOrRepeated
        {
            get
            {
                if (Count == 0) throw new InvalidOperationException();
                
                foreach(var repeatPressableInput in this)
                {
                    if(repeatPressableInput.WasJustPressedOrRepeated)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
    }
}

