using System;
using System.Collections.Generic;

namespace FlatRedBall.Input
{
	public interface IPressableInput
	{
		bool IsDown { get; }
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

    public class MultiplePressableInputs : IPressableInput
    {
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

