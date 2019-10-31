using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TmxEditor.Controllers;

namespace TmxEditor.CommandsAndState
{
    public class ApplicationEvents : Singleton<ApplicationEvents>
    {
        public event Action WireframePanning;

        public event Action SelectedTilesetChanged;

        public void CallSelectedTilesetChanged()
        {
            if(SelectedTilesetChanged != null)
            {
                SelectedTilesetChanged();
            }
        }

        public void CallAfterWireframePanning()
        {

            if (WireframePanning != null)
            {
                WireframePanning();
            }
        }

    }
}
