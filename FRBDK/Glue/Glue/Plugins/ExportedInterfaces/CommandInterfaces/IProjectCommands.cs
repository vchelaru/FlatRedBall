using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.VSHelpers.Projects;
using System.Windows.Forms;

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

        void AddContentFileToProject(string absoluteFileName, bool saveProjects = true);

        void CopyToBuildFolder(ReferencedFileSave rfs);
        void CopyToBuildFolder(string absoluteSource);

        void AddDirectory(string folderName, TreeNode treeNodeToAddTo);

        string MakeAbsolute(string relativeFileName, bool forceAsContent);

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
        bool UpdateFileMembershipInProject(ProjectBase project, string fileName, bool useContentPipeline, bool shouldLink, string parentFile = null, bool recursive = true);
    }
}
