using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.VSHelpers.Projects
{
    public class KniWebProject : VisualStudioProject
    {
        public override string ProjectId { get { return "KniWeb"; } }
        public override string PrecompilerDirective { get { return "BLAZORGL"; } }
        public override bool AllowContentCompile => true;

        public override string ContentDirectory => "Content/";

        public override string FolderName => "KniWeb";

        protected override bool NeedToSaveContentProject => false;

        public override string NeededVisualStudioVersion => "17.9";

        public KniWebProject(Project project) : base(project)
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

        public override bool IsFrbSourceLinked()
        {
            foreach (var item in this.Project.AllEvaluatedItems)
            {
                if (item.ItemType == "ProjectReference")
                {
                    var filename = System.IO.Path.GetFileName(item.EvaluatedInclude);
                    if (filename == "FlatRedBallKniWeb.csproj")
                    {
                        return true;
                    }
                }

            }
            return false;
        }
    }
}
