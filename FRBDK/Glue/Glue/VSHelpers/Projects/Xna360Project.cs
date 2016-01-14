using System;
using System.Collections.Generic;
using FlatRedBall.IO;
using Microsoft.Build.BuildEngine;

namespace FlatRedBall.Glue.VSHelpers.Projects
{
    public class Xna360Project : VisualStudioProject
    {
        public Xna360Project(Project project) : base(project)
        {
        }

        public override string ProjectId { get { return "Xna360"; } }

        public override string PrecompilerDirective { get { throw new NotImplementedException(); } }

        public override List<string> LibraryDlls
        {
            get
            {
                return new List<string>
                           {
                               @"Xna360\FlatRedBall.Content.dll",
                               @"Xna360\FlatRedBall.dll",
                               @"Xna360\Content\FlatRedBall.Content.dll",
                               @"Xna360\Content\FlatRedBall.dll"
                           };
            }
        }

        public override string FolderName
        {
            get { return "Xna360"; }
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
