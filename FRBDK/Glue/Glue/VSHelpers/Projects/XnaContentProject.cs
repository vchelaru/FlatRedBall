using System;
using System.Collections.Generic;
using Microsoft.Build.BuildEngine;

namespace FlatRedBall.Glue.VSHelpers.Projects
{
    public class XnaContentProject : VisualStudioProject
    {
        public XnaContentProject(Project project) : base(project)
        {
        }

        public override string ProjectId { get { return "XnaContent"; } }

        public override string PrecompilerDirective { get { throw new NotImplementedException(); } }

        public override List<string> LibraryDlls
        {
            get { return new List<string>();}
        }

        public override string FolderName
        {
            get { return "XnaContent"; }
        }

        protected override void LoadContentProject()
        {
        }

        public override string NeededVisualStudioVersion
        {
            get { return "9.0"; }
        }
    }
}
