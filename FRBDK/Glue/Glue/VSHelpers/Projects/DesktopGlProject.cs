using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;

namespace FlatRedBall.Glue.VSHelpers.Projects
{
    public class DesktopGlProject : VisualStudioProject
    {
        public override string ProjectId { get { return "DesktopGl"; } }
        public override string PrecompilerDirective { get { return "DESKTOP_GL"; } }

        public override bool AllowContentCompile
        {
            get { return false; }
        }

        public override List<string> LibraryDlls
        {
            get
            {
                return new List<string>
                {
                    "FlatRedBallDesktopGL.dll"
                };
            }
        }


        public override string ContentDirectory
        {
            get { return "Content/"; }
        }

        public override string FolderName
        {
            get
            {
                return "DesktopGl";
            }
        }

        protected override bool NeedToSaveContentProject { get { return false; } }

        public override string NeededVisualStudioVersion
        {
            get { return "14.0"; }

        }

        public DesktopGlProject(Project project) : base(project)
        {
        }

        public override string ProcessInclude(string path)
        {
            var returnValue = base.ProcessInclude(path);

            return returnValue.ToLowerInvariant();
        }

        public override string ProcessLink(string path)
        {
            var returnValue = base.ProcessLink(path);
            // Linux is case-sensitive
            return returnValue.ToLowerInvariant();
        }
    }
}
