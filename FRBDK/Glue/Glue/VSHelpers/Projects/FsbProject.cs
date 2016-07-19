using Microsoft.Build.Evaluation;
using System;
using System.Collections.Generic;

namespace FlatRedBall.Glue.VSHelpers.Projects
{
    public class FsbProject : CombinedEmbeddedContentProject
    {
        public FsbProject(Project project) : base(project)
        {
        }

        public override string ProjectId { get { return "FSB"; } }

        public override string PrecompilerDirective { get { return "SILVERLIGHT"; } }

        public override string FolderName
        {
            get { return "Fsb"; }
        }



        protected override bool ShouldIgnoreFile(string fileName)
        {
            if (base.ShouldIgnoreFile(fileName))
                return true;

            if (fileName == "Program.cs")
                return true;

            return false;
        }

        public override string NeededVisualStudioVersion
        {
            get { return "10.0"; }
        }




        public override List<string> LibraryDlls
        {
            get
            {
                return new List<string>
                           {
                               @"Silverlight\FlatRedBall.dll",
                               @"Silverlight\SilverArcade.SilverSprite.Core.dll",
                               @"Silverlight\SilverArcade.SilverSprite.dll"
                           };
            }
        }


    }
}
