using FlatRedBall.Glue.SaveClasses;
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

        void AddDirectory(string folderName, TreeNode treeNodeToAddTo);

        string MakeAbsolute(string relativeFileName, bool forceAsContent);
    }
}
