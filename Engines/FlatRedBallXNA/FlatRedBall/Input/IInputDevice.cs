using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Input
{
    /// <summary>
    /// Implementation for hardware input object which provides common input types such as movement and 
    /// primary/secondary actions.
    /// </summary>
    public interface IInputDevice
    {
        /// <summary>
        /// The default input for 2D movement, such as an analog stick on a gamepad.
        /// </summary>
        I2DInput Default2DInput { get; }

        /// <summary>
        /// The default input for moving up, such as the up direction on an analog stick on a gamepad.
        /// </summary>
        IRepeatPressableInput DefaultUpPressable { get; }

        /// <summary>
        /// The default input for moving down, such as the down direction on an analog stick on a gamepad.
        /// </summary>
        IRepeatPressableInput DefaultDownPressable { get; }

        /// <summary>
        /// The default input for moving left, such as the left direction on an analog stick on a gamepad.
        /// </summary>
        IRepeatPressableInput DefaultLeftPressable { get; }

        /// <summary>
        /// The default input for moving right, such as the right direction on an analog stick on a gamepad.
        /// </summary>
        IRepeatPressableInput DefaultRightPressable { get; }

        /// <summary>
        /// The default input for moving horizontally, like in a platformer. For example, this may be 
        /// the left/right directions on an analog stick on a gamepad.
        /// </summary>
        I1DInput DefaultHorizontalInput { get; }

        /// <summary>
        /// The default input for moving vertically, like climbing a ladder in a platformer. For example, this may be
        /// the up/down direction on an analog stick on a gamepad.
        /// </summary>
        I1DInput DefaultVerticalInput { get; }

        /// <summary>
        /// The default primary action input used by a game like jumping. For example,
        /// this may be the A button on a gamepad.
        /// </summary>
        IPressableInput DefaultPrimaryActionInput { get; }

        /// <summary>
        /// The default secondary action input used by a game like shooting in a platformer. For example,
        /// this may be the X button on a gamepad.
        /// </summary>
        IPressableInput DefaultSecondaryActionInput { get; }

        /// <summary>
        /// The default input used by a game to confirm an action like a menu button. For example,
        /// this may be the start button on a gamepad.
        /// </summary>
        IPressableInput DefaultConfirmInput { get; }

        /// <summary>
        /// The default input used by a game to have a player join the game. For example,
        /// this may be the start button on a gamepad.
        /// </summary>
        IPressableInput DefaultJoinInput { get;  }

        /// <summary>
        /// the default input used to puase the game. For example,
        /// this may be the escape key on a keyboard.
        /// </summary>
        IPressableInput DefaultPauseInput { get;  }

        /// <summary>
        /// The default input to indicate a "back" action. For example,
        /// this may be the Back button on a gamepad.
        /// </summary>
        IPressableInput DefaultBackInput { get;  }

        /// <summary>
        /// The default input to indicate a cancel action. For example, this
        /// may be the B button on a gamepad;
        /// </summary>
        IPressableInput DefaultCancelInput { get;  }
    }
}
