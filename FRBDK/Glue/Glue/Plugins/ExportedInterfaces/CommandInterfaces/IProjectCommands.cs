using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.VSHelpers.Projects;
using System.Collections.Generic;


namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    public interface IProjectCommands
    {
        void SaveProjects();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="element"></param>
        /// <param name="partialName"></param>
        /// <param name="code"></param>
        void CreateAndAddPartialFile(IElement element, string partialName, string code);

        /// <summary>
        /// Creates an empty code file (if it doesn't already exist), and adds it to the main project. If the file already exists, then it 
        /// is not modified on disk, but is still added to the code project. The code project is saved if added.
        /// </summary>
        /// <param name="relativeFileName">The file name.</param>
        void CreateAndAddCodeFile(string relativeFileName);

        /// <summary>
        /// Creates an empty code file (if it doesn't already exist), and adds it to the main project. If the file already exists, then it 
        /// is not modified on disk, but is still added to the code project. The code project is saved if added.
        /// </summary>
        /// <param name="filePath">The file path to save.</param>
        void CreateAndAddCodeFile(FilePath filePath);

        void AddContentFileToProject(string absoluteFileName, bool saveProjects = true);

        void CopyToBuildFolder(ReferencedFileSave rfs);
        void CopyToBuildFolder(string absoluteSource);

#if GLUE
        void AddDirectory(string folderName, System.Windows.Forms.TreeNode treeNodeToAddTo);
#endif

        string MakeAbsolute(string relativeFileName, bool forceAsContent);

        void MakeGeneratedCodeItemsNested();

        void RemoveFromProjects(FilePath filePath);
        void RemoveFromProjects(string absoluteFileName);

        bool UpdateFileMembershipInProject(ReferencedFileSave referencedFileSave);

        /// <summary>
        /// Updates the argument fileName's membership to the argument project.
        /// </summary>
        /// <param name="project">The project (main, does not have to be a content project if XNA)</param>
        /// <param name="fileName">The file name, which can be relative to the project or which can be absolute.</param>
        /// <param name="useContentPipeline">Whether to force the file to use the content pipeline.</param>
        /// <param name="shouldLink"></param>
        /// <param name="parentFile"></param>
        /// <returns></returns>
        bool UpdateFileMembershipInProject(ProjectBase project, string fileName, bool useContentPipeline, bool shouldLink, string parentFile = null, bool recursive = true, List<string> alreadyReferencedFiles = null);
    }
}
