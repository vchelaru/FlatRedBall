using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.VSHelpers.Projects
{
    public class VisualStudioDotNetStandardProject : VisualStudioProject
    {
        public override string NeededVisualStudioVersion => "16.0";

        public ProjectBase StandardProject
        {
            get; private set;
        }

        public override ProjectBase ContentProject
        {
            get => StandardProject;
            set => throw new NotImplementedException();
        }

        public override List<string> LibraryDlls => new List<string>
        {
            "FlatRedBallDesktopGL.dll"
        };

        public override string FolderName => "Standard";

        public override string ProjectId => "Standard";

        public override string PrecompilerDirective => "Standard";

        public VisualStudioDotNetStandardProject(Project project) : base(project)
        {

        }
    }
}
