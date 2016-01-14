using System;
using System.Collections.Generic;
using FlatRedBall.IO;
using Microsoft.Build.BuildEngine;

namespace FlatRedBall.Glue.VSHelpers.Projects
{
    public class Xna4_360Project : VisualStudioProject
    {
        public Xna4_360Project(Project project) : base(project)
        {
        }

        public override string ProjectId { get { return "Xna4_360"; } }

        public override string PrecompilerDirective { get { return "XBOX360";} }

        public override List<string> LibraryDlls
        {
            get 
            { 
                return new List<string>
                           {
                               @"Xna4_360\FlatRedBall.dll"
                           };
            }
        }

        public override string FolderName
        {
            get { return "Xna360"; }
        }


        protected override string ContentProjectDirectory
        {
            get
            {
                var contentDirectory = FileManager.GetDirectory(Directory);
                contentDirectory += Name + "Content";

                return contentDirectory;
            }
        }

        protected override void LoadContentProject()
        {
            List<string> files = FileManager.GetAllFilesInDirectory(ContentProjectDirectory, "contentproj", 0);

            if (files.Count != 0)
            {
                ContentProject = ProjectCreator.LoadXnaProjectFor(this, files[0]) ;
            }
            else
            {
                ContentProject = this;
            }
        }

        public override string NeededVisualStudioVersion
        {
            get { return "10.0"; }
        }
    }
}
