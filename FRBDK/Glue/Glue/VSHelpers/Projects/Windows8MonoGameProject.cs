using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.BuildEngine;

namespace FlatRedBall.Glue.VSHelpers.Projects
{
    public class Windows8MonoGameProject : CombinedEmbeddedContentProject
    {
        public Windows8MonoGameProject(Project project)
            : base(project)
        {
            // This is temporary - eventually we need a better solution for this:
            // Update October 1, 2012
            // I wanted to integrate this
            // into Glue, but it seems like
            // something that is more plugin-worthy.
            // I'm going to continue to make these ignored
            // and we'll add them later through a plugin.
            this.ExtensionsToIgnore.Add("mp3");
            this.ExtensionsToIgnore.Add("wav");


        }

        public override bool AllowContentCompile
        {
            get { return false; }
        }

        public override string ProjectId
        {
            get { return "Windows8 MonoGame"; }
        }

        public override List<string> LibraryDlls
        {
            get 
            {
                return new List<string>
                {
                    "FlatRedBallWindows8.dll",
                    "MonoGame.Framework.Windows8.dll"
                };

            
            }
        }

        public override string FolderName
        {
            get 
            {
                return "Windows8";
            }
        }

        public override string PrecompilerDirective { get { return "WINDOWS_8"; } }



        public override string NeededVisualStudioVersion
        {
            get { return "11.0"; }
        }



    }
}
