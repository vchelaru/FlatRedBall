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
            get { return "UWP MonoGame"; }
        }

        public override List<string> LibraryDlls
        {
            get
            {
                return new List<string>
                {
                    "FlatRedBallUwp.dll"
                };
            }
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
