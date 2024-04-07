using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.VSHelpers.Projects
{
    public abstract class MonoGameDesktopGlBaseProject : VisualStudioProject
    {
        public override string ProjectId { get { return "DesktopGl"; } }
        public override string PrecompilerDirective { get { return "DESKTOP_GL"; } }

        public override bool AllowContentCompile => false;

        public override string ContentDirectory => "Content/";


        public override string FolderName => "DesktopGl";
            
        protected override bool NeedToSaveContentProject  => false;

        public MonoGameDesktopGlBaseProject(Project project) : base(project)
        {
        }

        public override string ProcessInclude(string path)
        {
            var returnValue = base.ProcessInclude(path);

            return returnValue;
        }

        public override string ProcessLink(string path)
        {
            var returnValue = base.ProcessLink(path);
            // Linux is case-sensitive
            return returnValue;
        }
    }
}
