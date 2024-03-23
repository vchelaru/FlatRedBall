using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.VSHelpers.Projects
{
    public class Windows8MonoGameProject : CombinedEmbeddedContentProject
    {
        public Windows8MonoGameProject(Project project)
            : base(project)
        {
        }

        public override bool AllowContentCompile
        {
            get { return false; }
        }

        public override string ProjectId
        {
            get { return "Windows8 MonoGame"; }
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
