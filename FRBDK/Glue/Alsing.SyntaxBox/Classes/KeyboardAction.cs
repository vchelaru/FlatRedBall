// *
// * Copyright (C) 2008 Roger Alsing : http://www.RogerAlsing.com
// *
// * This library is free software; you can redistribute it and/or modify it
// * under the terms of the GNU Lesser General Public License 2.1 or later, as
// * published by the Free Software Foundation. See the included license.txt
// * or http://www.gnu.org/copyleft/lesser.html for details.
// *
// *

using System.Windows.Forms;

namespace Alsing.Windows.Forms.SyntaxBox
{
    /// <summary>
    /// Delegate used when triggering keyboard actions
    /// </summary>
    public delegate void ActionDelegate();

    /// <summary>
    /// Instances of this class represents a relation between pressed keys and a delegate
    /// </summary>
    public class KeyboardAction
    {
        /// <summary>
        /// Determines what key to associate with the action
        /// </summary>
        private Keys _Key = 0;

        public KeyboardAction() {}

        public KeyboardAction(Keys key, bool shift, bool control, bool alt, bool
                                                                                allowreadonly,
                              ActionDelegate actionDelegate)
        {
            Key = key;
            Control = control;
            Alt = alt;
            Shift = shift;
            Action = actionDelegate;
            AllowReadOnly = allowreadonly;
        }

        public bool Shift { get; set; }

        public bool Alt { get; set; }

        public bool Control { get; set; }

        public bool AllowReadOnly { get; set; }

        public Keys Key
        {
            get { return _Key; }
            set { _Key = value; }
        }

        public ActionDelegate Action { get; set; }
    }
}