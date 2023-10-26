using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.VSHelpers.Projects
{
    public class FnaDesktopProject : VisualStudioProject
    {
        public override string ProjectId { get { return "FnaDesktop"; } }
        public override string PrecompilerDirective { get { return "FNA"; } }
        public override bool AllowContentCompile => false;

        public override List<string> LibraryDlls
        {
            get
            {
                return new List<string>
                {
                    "FlatRedBall.FNA.dll"
                };
            }
        }

        public override string ContentDirectory => "Content/";

        public override string FolderName => "FNA";

        protected override bool NeedToSaveContentProject => false;

        public override string NeededVisualStudioVersion => "17.0";

        public FnaDesktopProject(Project project) : base(project)
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
