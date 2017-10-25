using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Gui.Controls
{
    /// <summary>
    /// An object which can be selected, such as a text box which can be be selected
    /// to receive input, or a button which can be selected when tabbing through a list
    /// of buttonsn to recieve the click event by pressing the Enter key.
    /// </summary>
    public interface ISelectable
    {
        bool IsSelected { get; set; }

        event EventHandler IsSelectedChanged;
    }
}
