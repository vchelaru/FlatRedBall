using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;

namespace FlatRedBall.Glue.VSHelpers.Projects
{
    public class DesktopGlLinuxProject : DesktopGlProject
    {

        public override string ProjectId { get { return "DesktopGlLinux"; } }
        public override string PrecompilerDirective { get { return "LINUX"; } }

        public DesktopGlLinuxProject(Project project) : base(project)
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
