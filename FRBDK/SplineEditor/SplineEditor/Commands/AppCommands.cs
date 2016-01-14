using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SplineEditor.Commands
{
    public class AppCommands : Singleton<AppCommands>
    {

        public FileCommands File
        {
            get;
            private set;
        }

        public EditCommands Edit
        {
            get;
            private set;
        }

        public GuiCommands Gui
        {
            get;
            private set;
        }

        public PreviewCommands Preview
        {
            get;
            private set;
        }

        public AppCommands()
        {
            File = new FileCommands();
            Edit = new EditCommands();
            Gui = new GuiCommands();
            Preview = new PreviewCommands();
        }
    }
}
