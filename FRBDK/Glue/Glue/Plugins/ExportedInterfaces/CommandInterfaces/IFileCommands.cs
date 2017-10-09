using EditorObjects.Parsing;
using FlatRedBall.Glue.SaveClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    public interface IFileCommands
    {
        IEnumerable<string> GetFilesReferencedBy(string absoluteName, EditorObjects.Parsing.TopLevelOrRecursive topLevelOrRecursive);

        IEnumerable<string> GetFilesReferencedBy(ReferencedFileSave file, EditorObjects.Parsing.TopLevelOrRecursive topLevelOrRecursive);

        IEnumerable<string> GetAllFilesNeededOnDisk();

        List<string> GetAllReferencedFileNames();

        List<string> GetAllReferencedFileNames(TopLevelOrRecursive topLevelOrRecursive);

        void ClearFileCache(string absoluteName);

        string GetContentFolder(IElement element);

        void IgnoreNextChangeOnFile(string absoluteFileName);

        string GetFullFileName(ReferencedFileSave rfs);
        ReferencedFileSave GetReferencedFile(string absoluteFile);
    }
}
