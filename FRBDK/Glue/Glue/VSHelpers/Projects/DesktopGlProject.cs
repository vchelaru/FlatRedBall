using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;

namespace FlatRedBall.Glue.VSHelpers.Projects
{
    class DesktopGlProject : VisualStudioProject
    {
        public override string ProjectId { get { return "DesktopGl"; } }
        public override string PrecompilerDirective { get { return "DESKTOP_GL"; } }

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
    }
}
