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
            set { }// do nothing
        }

        public override string FolderName => "Standard";

        public override string ProjectId => "Standard";

        public override string PrecompilerDirective => "Standard";

        public VisualStudioDotNetStandardProject(Project project, Project standardProject) : base(project)
        {
            StandardProject = new ClassLibraryProject(standardProject);
        }
    }
}
