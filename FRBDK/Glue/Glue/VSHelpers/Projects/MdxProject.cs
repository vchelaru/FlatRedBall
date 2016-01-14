using System;
using System.Collections.Generic;
using Microsoft.Build.BuildEngine;

namespace FlatRedBall.Glue.VSHelpers.Projects
{
    public class MdxProject : VisualStudioProject
    {
        public MdxProject(Project project) : base(project)
        {
        }

        public override string ProjectId { get { return "Mdx"; } }

        public override string ContentDirectory
        {
            get
            {
                return "Content/";
            }
        }

        public override List<string> LibraryDlls
        {
            get
            {
                return new List<string>
                           {
                               "FlatRedBallMdx.dll"
                           };
            }
        }

        public override string FolderName
        {
            get { return "Mdx"; }
        }

        public override string PrecompilerDirective { get { return "FRB_MDX";} }


        public override string NeededVisualStudioVersion
        {
            get { return "9.0"; }
        }
    }
}
