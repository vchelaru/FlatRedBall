using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ParticleEditor.Managers;

namespace ParticleEditor
{
    public class AppCommands : Singleton<AppCommands>
    {

        FileCommands mFile;

        public FileCommands File
        {
            get { return mFile; }
        }

        public AppCommands()
        {
            mFile = new FileCommands();
        }

    }
}
