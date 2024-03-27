using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.VSHelpers.Projects
{
    public class UwpProject : CombinedEmbeddedContentProject
    {
        public UwpProject(Project project)
            : base(project)
        {
        }


        public override bool AllowContentCompile
        {
            get { return false; }
        }

        public override string ProjectId
        {
            get { return "UWP MonoGame"; }
        }

        public override string FolderName
        {
            get
            {
                return "UWP";
            }
        }

        public override string PrecompilerDirective { get { return "UWP"; } }

        public override string NeededVisualStudioVersion
        {
            get { return "14.0"; }
        }
    }
}
