// *
// * Copyright (C) 2008 Roger Alsing : http://www.RogerAlsing.com
// *
// * This library is free software; you can redistribute it and/or modify it
// * under the terms of the GNU Lesser General Public License 2.1 or later, as
// * published by the Free Software Foundation. See the included license.txt
// * or http://www.gnu.org/copyleft/lesser.html for details.
// *
// *

using System;
using System.Windows.Forms;

namespace Alsing.Windows.Forms
{

    #region params

    public class NativeMessageArgs : EventArgs
    {
        public bool Cancel;
        public Message Message;
    }

    public delegate void NativeMessageHandler(object s, NativeMessageArgs e);

    #endregion

    public class NativeSubclasser : NativeWindow
    {
        public NativeSubclasser() {}

        public NativeSubclasser(Control Target)
        {
            AssignHandle(Target.Handle);
            Target.HandleCreated += Handle_Created;
            Target.HandleDestroyed += Handle_Destroyed;
        }

        public NativeSubclasser(IntPtr hWnd)
        {
            AssignHandle(hWnd);
        }

        public event NativeMessageHandler Message = null;

        protected virtual void OnMessage(NativeMessageArgs e)
        {
            if (Message != null)
                Message(this, e);
        }

        private void Handle_Created(object o, EventArgs e)
        {
            AssignHandle(((Control) o).Handle);
        }

        private void Handle_Destroyed(object o, EventArgs e)
        {
            ReleaseHandle();
        }

        public void Detatch()
        {
            //	this.ReleaseHandle ();
        }

        protected override void WndProc(ref Message m)
        {
            try
            {
                var e = new NativeMessageArgs {Message = m, Cancel = false};

                OnMessage(e);

                if (!e.Cancel)
                    base.WndProc(ref m);
            }
            catch (Exception x)
            {
                Console.WriteLine(x.Message);
            }
        }
    }
}