using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using FlatRedBall.IO;

namespace FlatRedBallProfiler.ViewModels
{
    public class ContentViewModel
    {
        object backingData;

        string executableNameButUsePropertyPlease;

        public string ExecutableName
        {
            get
            {
                if(string.IsNullOrEmpty(executableNameButUsePropertyPlease))
                {
                    var assembly = Assembly.GetEntryAssembly();

                    executableNameButUsePropertyPlease = assembly.Location;
                }

                return executableNameButUsePropertyPlease;
            }
        }

        public string Name
        {
            get
            {
                var name = backingData.ToString();

                if (string.IsNullOrEmpty(name))
                {
                    name = backingData.GetType().ToString();
                }

                string directory = FileManager.GetDirectory(ExecutableName);

                if (FileManager.IsRelativeTo(name, directory))
                {
                    name = FileManager.MakeRelative(name, directory);
                }

                return name;
            }
        }

        public ContentManagerViewModel Parent 
        { 
            get; 
            set; 
        }

        public ContentViewModel(object contentItem)
        {
            backingData = contentItem;
        }

        public void Open()
        {
            // See if the Name is a file:
            string candidateFile = backingData.ToString();

            if(!string.IsNullOrEmpty(candidateFile))
            {
                var exists = System.IO.File.Exists(candidateFile);

                if(exists)
                {
                    System.Diagnostics.Process.Start(candidateFile);
                }
            }
        }

    }
}
