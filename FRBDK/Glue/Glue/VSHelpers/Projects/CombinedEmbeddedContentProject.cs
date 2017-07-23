using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.VSHelpers.Projects
{
    /// <summary>
    /// A CombinedEmbeddedContentProject is a project
    /// which has has code and content combined into one
    /// project, and which uses embedded content in the project
    /// (the build action is Content).
    /// </summary>
    public abstract class CombinedEmbeddedContentProject : VisualStudioProject
    {
        public CombinedEmbeddedContentProject(Project project)
            : base(project)
        {


        }

        protected override bool NeedToSaveContentProject { get { return false; } }
        public override bool ContentCopiedToOutput { get { return false; } }

        public override string ContentDirectory
        {
            get { return "Content/"; }
        }


        public override string DefaultContentAction { get { return "Content"; } }


    }
}
