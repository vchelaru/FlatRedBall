using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.VSHelpers.Projects;
using Microsoft.Build.BuildEngine;

namespace UnitTests
{
    class TestProject : ProjectBase
    {

        public override string FullFileName
        {
            get { return "FullName"; }
        }

        public override string Name
        {
            get { return "Name"; }
        }

        public override bool IsDirty
        {
            get { throw new NotImplementedException(); }
            set
            {
                // do nothing (for now?)
            }
        }

        public override Microsoft.Build.BuildEngine.BuildItemGroup EvaluatedItems
        {
            get { throw new NotImplementedException(); }
        }

        public override List<string> LibraryDlls
        {
            get { throw new NotImplementedException(); }
        }

        public override BuildItem AddContentBuildItem(string absoluteFile)
        {
            throw new NotImplementedException();
        }

        public override BuildItem AddContentBuildItem(string absoluteFile, SyncedProjectRelativeType relativityType, bool forceToContentPipeline)
        {
            throw new NotImplementedException();
        }

        public override void UpdateContentFile(string sourceFileName)
        {
            throw new NotImplementedException();
        }

        public override string FolderName
        {
            get { throw new NotImplementedException(); }
        }

        public override bool IsFilePartOfProject(string fileToUpdate, BuildItemMembershipType membershipType)
        {
            throw new NotImplementedException();
        }

        public override bool IsFilePartOfProject(string fileToUpdate, BuildItemMembershipType membershipType, bool relativeItem)
        {
            throw new NotImplementedException();
        }

        protected override void ForceSave(string fileName)
        {
            throw new NotImplementedException();
        }

        public override void Load(string fileName)
        {
            throw new NotImplementedException();
        }

        public override void MakeBuildItemNested(Microsoft.Build.BuildEngine.BuildItem item, string parent)
        {
            throw new NotImplementedException();
        }

        public override void SyncTo(ProjectBase projectBase, bool performTranslation)
        {
            throw new NotImplementedException();
        }

        protected override Microsoft.Build.BuildEngine.BuildItem AddCodeBuildItem(string fileName, bool isSyncedProject, string directoryToCreate)
        {
            throw new NotImplementedException();
        }

        public override string ProjectId
        {
            get { throw new NotImplementedException(); }
        }

        public override string PrecompilerDirective
        {
            get { throw new NotImplementedException(); }
        }

        protected override void RemoveItem(string itemName, Microsoft.Build.BuildEngine.BuildItem item)
        {
            throw new NotImplementedException();
        }
    }
}
