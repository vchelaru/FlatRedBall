using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;

namespace FlatRedBall.Glue.VSHelpers.Projects
{
    public class XnaContentProject : VisualStudioProject
    {
        public XnaContentProject(Project project) : base(project)
        {
        }

        public override string ProjectId { get { return "XnaContent"; } }

        public override string PrecompilerDirective { get { throw new NotImplementedException(); } }

        public override string FolderName
        {
            get { return "XnaContent"; }
        }
        

        public override string NeededVisualStudioVersion
        {
            get { return "9.0"; }
        }
    }
}
