using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Evaluation;

namespace FlatRedBall.Glue.VSHelpers.Projects
{
    /// <summary>
    ///  The goal here was to use the MG content pipeline...but it's not possible because it doesn't support linking.
    ///  I added the code for linking and it's maybe going to be merged and accepted into MG 3.7, but until then I just
    ///  don't think it will work...actually even then I'm not sure. I guess at that point I make my own parser and writer,
    ///  and don't link MG libs.
    /// </summary>
    public class ContentHybridProject : ProjectBase
    {
        public override IEnumerable<ProjectItem> EvaluatedItems
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override string FolderName
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override string FullFileName
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override bool IsDirty
        {
            get
            {
                throw new NotImplementedException();
            }

            set
            {
                throw new NotImplementedException();
            }
        }

        public override List<string> LibraryDlls
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override string Name
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override string PrecompilerDirective
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override string ProjectId
        {
            get
            {
                throw new NotImplementedException();
            }
        }
        
        public override ProjectItem AddContentBuildItem(string absoluteFile, SyncedProjectRelativeType relativityType, bool forceToContentPipeline)
        {
            throw new NotImplementedException();
        }

        public override bool IsFilePartOfProject(string fileToUpdate, BuildItemMembershipType membershipType)
        {
            throw new NotImplementedException();
        }

        public override bool IsFilePartOfProject(string fileToUpdate, BuildItemMembershipType membershipType, bool relativeItem)
        {
            throw new NotImplementedException();
        }

        public override void Load(string fileName)
        {
            throw new InvalidOperationException("Hybrid projects require 2 projects - monogame content and VS");
        }

        public override void MakeBuildItemNested(ProjectItem item, string parent)
        {
            throw new NotImplementedException();
        }

        public override void Save(string fileName)
        {
            throw new NotImplementedException();
        }

        public override void SyncTo(ProjectBase projectBase, bool performTranslation)
        {
            throw new NotImplementedException();
        }

        public override void Unload()
        {
            throw new NotImplementedException();
        }

        public override void UpdateContentFile(string sourceFileName)
        {
            throw new NotImplementedException();
        }

        protected override ProjectItem AddCodeBuildItem(string fileName, bool isSyncedProject, string directoryToCreate)
        {
            throw new NotImplementedException();
        }

        protected override void RemoveItem(string itemName, ProjectItem item)
        {
            throw new NotImplementedException();
        }
    }
}
