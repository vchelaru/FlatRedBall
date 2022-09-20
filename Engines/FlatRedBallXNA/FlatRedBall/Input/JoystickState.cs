using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Xna.Framework.Input
{

    public struct JoystickCapabilities
    {
        public bool IsConnected { get; internal set; }

        public string Identifier { get; internal set; }

        public bool IsGamepad { get; internal set; }

        public int AxisCount { get; internal set; }

        public int ButtonCount { get; internal set; }

        public int HatCount { get; internal set; }

        public static bool operator ==(JoystickCapabilities left, JoystickCapabilities right)
        {
            if (left.IsConnected == right.IsConnected && left.Identifier == right.Identifier && left.IsGamepad == right.IsGamepad && left.AxisCount == right.AxisCount && left.ButtonCount == right.ButtonCount)
            {
                return left.HatCount == right.HatCount;
            }

            return false;
        }

        public static bool operator !=(JoystickCapabilities left, JoystickCapabilities right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            if (obj is JoystickCapabilities)
            {
                return this == (JoystickCapabilities)obj;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return Identifier.GetHashCode();
        }

        public override string ToString()
        {
            return "[JoystickCapabilities: IsConnected=" + IsConnected + ", Identifier=" + Identifier + ", IsGamepad=" + IsGamepad + " , AxisCount=" + AxisCount + ", ButtonCount=" + ButtonCount + ", HatCount=" + HatCount + "]";
        }
    }
    // These are here just to match MonoGame for compile issues.
    public struct JoystickHat
    {
        public ButtonState Down { get; internal set; }

        public ButtonState Left { get; internal set; }

        public ButtonState Right { get; internal set; }

        public ButtonState Up { get; internal set; }

        public static bool operator ==(JoystickHat left, JoystickHat right)
        {
            if (left.Down == right.Down && left.Left == right.Left && left.Right == right.Right)
            {
                return left.Up == right.Up;
            }

            return false;
        }

        public static bool operator !=(JoystickHat left, JoystickHat right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            if (obj is JoystickHat)
            {
                return this == (JoystickHat)obj;
            }

            return false;
        }

        public override int GetHashCode()
        {
            int num = 0;
            if (Left == ButtonState.Pressed)
            {
                num |= 8;
            }

            if (Up == ButtonState.Pressed)
            {
                num |= 4;
            }

            if (Right == ButtonState.Pressed)
            {
                num |= 2;
            }

            if (Down == ButtonState.Pressed)
            {
                num |= 1;
            }

            return num;
        }

        public override string ToString()
        {
            return string.Concat((int)Left, (int)Up, (int)Right, (int)Down);
        }
    }
    public struct JoystickState
    {
        public bool IsConnected { get; internal set; }

        public int[] Axes { get; internal set; }

        public ButtonState[] Buttons { get; internal set; }

        public JoystickHat[] Hats { get; internal set; }

        public static bool operator ==(JoystickState left, JoystickState right)
        {
            if (left.IsConnected == right.IsConnected && left.Axes.SequenceEqual(right.Axes) && left.Buttons.SequenceEqual(right.Buttons))
            {
                return left.Hats.SequenceEqual(right.Hats);
            }

            return false;
        }

        public static bool operator !=(JoystickState left, JoystickState right)
        {
            return !(left == right);
        }

        public override bool Equals(object obj)
        {
            if (obj is JoystickState)
            {
                return this == (JoystickState)obj;
            }

            return false;
        }

        public override int GetHashCode()
        {
            int num = 0;
            if (IsConnected)
            {
                int[] axes = Axes;
                foreach (int num2 in axes)
                {
                    num = (num * 397) ^ num2;
                }

                for (int j = 0; j < Buttons.Length; j++)
                {
                    num ^= (int)Buttons[j] << j % 32;
                }

                JoystickHat[] hats = Hats;
                for (int k = 0; k < hats.Length; k++)
                {
                    JoystickHat joystickHat = hats[k];
                    num = (num * 397) ^ joystickHat.GetHashCode();
                }
            }

            return num;
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder(52 + Axes.Length * 7 + Buttons.Length + Hats.Length * 5);
            stringBuilder.Append("[JoystickState: IsConnected=" + (IsConnected ? 1 : 0));
            if (IsConnected)
            {
                stringBuilder.Append(", Axes=");
                int[] axes = Axes;
                for (int i = 0; i < axes.Length; i++)
                {
                    int num = axes[i];
                    stringBuilder.Append(((num > 0) ? "+" : "") + num.ToString("00000") + " ");
                }

                stringBuilder.Length--;
                stringBuilder.Append(", Buttons=");
                ButtonState[] buttons = Buttons;
                foreach (ButtonState value in buttons)
                {
                    stringBuilder.Append((int)value);
                }

                stringBuilder.Append(", Hats=");
                JoystickHat[] hats = Hats;
                foreach (JoystickHat joystickHat in hats)
                {
                    stringBuilder.Append(string.Concat(joystickHat, " "));
                }

                stringBuilder.Length--;
            }

            stringBuilder.Append("]");
            return stringBuilder.ToString();
        }
    }
}
