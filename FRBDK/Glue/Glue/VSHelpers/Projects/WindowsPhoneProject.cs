using System;
using System.Collections.Generic;
using FlatRedBall.IO;
using Microsoft.Build.Evaluation;

namespace FlatRedBall.Glue.VSHelpers.Projects
{
    public class WindowsPhoneProject : VisualStudioProject
    {
        public WindowsPhoneProject(Project project) : base(project)
        {
        }

        public override string ProjectId { get { return "WindowsPhone"; } }

        public override string PrecompilerDirective { get { throw new NotImplementedException(); } }

        public override string FolderName
        {
            get { return "WindowsPhone"; }
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

        public override void LoadContentProject()
        {
            // We're slowly getting rid of WinPhone 7
            throw new NotImplementedException();
            //List<string> files = FileManager.GetAllFilesInDirectory(ContentProjectDirectory, "contentproj", 0);

            //if (files.Count != 0)
            //{
            //    ContentProject = ProjectCreator.LoadXnaProjectFor(this, files[0]);
            //}
            //else
            //{
            //    ContentProject = this;
            //}
        }

        public override string NeededVisualStudioVersion
        {
            get { return "10.0"; }
        }
    }
}
