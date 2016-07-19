using System;
using System.Collections.Generic;
using FlatRedBall.IO;
using Microsoft.Build.Evaluation;

namespace FlatRedBall.Glue.VSHelpers.Projects
{
    public class XnaProject : VisualStudioProject
    {
        public XnaProject(Project project) : base(project)
        {
        }

        public override string ProjectId { get { return "Xna3_1"; } }

        public override string PrecompilerDirective { get { return "XNA3";} }

        public override List<string> LibraryDlls
        {
            get 
            { 
                return new List<string>
                           {
                               @"XnaPc\FlatRedBall.Content.dll",
                               @"XnaPc\FlatRedBall.dll"
                           };
            }
        }

        public override string FolderName
        {
            get { return "Xna"; }
        }

        protected override string ContentProjectDirectory
        {
            get { return Directory + "Content/"; }
        }

        protected override void LoadContentProject()
        {
            List<string> files = FileManager.GetAllFilesInDirectory(ContentProjectDirectory, "contentproj", 0);

            if (files.Count != 0)
            {
                ContentProject = ProjectCreator.LoadXnaProjectFor(this, files[0]);
            }
            else
            {
                ContentProject = this;
            }
        }

        public override string NeededVisualStudioVersion
        {
            get { return "9.0"; }
        }
    }
}
