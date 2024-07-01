using System;

using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace FlatRedBall.Gui
{
    /// <summary>
    /// Event raised by the Keyboard every frame on the InputManager's ReceivingInput reference.
    /// </summary>
    /// <param name="inputReceiver">The current IInputReceiver.</param>
    public delegate void FocusUpdateDelegate(IInputReceiver inputReceiver);

    /// <summary>
	/// Interface for objects which can receive input from the InputManager.
	/// </summary>
	/// <remarks>
	/// 
	/// This interface should only be used for Gui elements specifically because the GuiManager will change the InputManager.ReceivingInput
	/// reference depending on the activity of the cursor and other Gui elements.  The InputManager will only keep track of one IInputReceiver
	/// at a time, and each IInputReceiver needs to assign itself as the target for input through the static InputManager.ReceivingInput field.
	/// 
	/// <para>Since this is only used for Gui elements, this interface is rarely used in games.</para>
    /// </remarks>
    public interface IInputReceiver
    {
        #region Properties

        List<Keys> IgnoredKeys
        {
            get;
        }

        /// <summary>
		/// A method which determines whether the instance can currently receive focus as an input receiver.
		/// </summary>
        /// <returns>Whether the instance is taking input.</returns>
        bool TakingInput
		{
			get;

        }

        /// <summary>
        /// The next IInputReceiver in the tab sequence.  In other words, if this element is currently
        /// receiving input (is the InputManager's ReceivingInput), pressing tab will set the NextInTabSequence
        /// to be the InputManager's ReceivingInput.
        /// </summary>
        IInputReceiver NextInTabSequence
        {
            get;
            set;
        }

        #endregion

        #region Events

        /// <summary>
        /// Event raised every frame if there is a non-null InputManager.ReceivingInput.  This allows
        /// the IInputReceiver to perform custom every-frame logic when it has focus, such as a ListBox
        /// listening for the Delete key to delete highlighted elements.
        /// </summary>
        event FocusUpdateDelegate FocusUpdate;
        #endregion

        #region Methods

        /// <summary>
        /// Called by the InputManager automatically every frame.
        /// </summary>
        /// <remarks>
        /// The implementation of this method should raise the FocusUpdate event.
        /// </remarks>
        void OnFocusUpdate();

        /// <summary>
        /// Called by the engine automatically when an IInputReceiver gains focus.
        /// </summary>
        /// <remarks>
        /// The implementation of this method should raise the GainFocus event.
        /// </remarks>
        void OnGainFocus();

        /// <summary>
        /// Called by the engine automatically when an IInputReceiver loses focus.
        /// </summary>
        void LoseFocus();

        /// <summary>
		/// The method called every frame by the InputManager in the Update method
        /// if this is the IInputReceiver referenced by the InputManager.  This does
        /// not have to be called automatically.
        /// </summary>
        [Obsolete("Use OnFocusUpdate intstead")]
        void ReceiveInput();

        void HandleKeyDown(Keys key, bool isShiftDown, bool isAltDown, bool isCtrlDown);

        void HandleCharEntered(char character);

        #endregion

    }
}
