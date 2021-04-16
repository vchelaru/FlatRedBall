using EditorObjects.Parsing;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.SetVariable;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    public interface IFileCommands
    {
        GeneralResponse GetLastParseResponse(FilePath file);

        IEnumerable<FilePath> GetFilesReferencedBy(string absoluteName, EditorObjects.Parsing.TopLevelOrRecursive topLevelOrRecursive);
        IEnumerable<FilePath> GetFilePathsReferencedBy(string absoluteName, EditorObjects.Parsing.TopLevelOrRecursive topLevelOrRecursive);

        IEnumerable<FilePath> GetFilesReferencedBy(ReferencedFileSave file, EditorObjects.Parsing.TopLevelOrRecursive topLevelOrRecursive);

        IEnumerable<FilePath> GetAllFilesNeededOnDisk();

        List<FilePath> GetAllReferencedFileNames();
        List<FilePath> GetAllReferencedFilePaths();

        List<FilePath> GetAllReferencedFileNames(TopLevelOrRecursive topLevelOrRecursive);

        void ClearFileCache(string absoluteName);

        string GetContentFolder(IElement element);
        FilePath GetGlobalContentFolder();

        void IgnoreNextChangeOnFile(string absoluteFileName);

        string GetFullFileName(ReferencedFileSave rfs);
        FilePath GetFilePath(ReferencedFileSave rfs);
        ReferencedFileSave GetReferencedFile(string absoluteFile);

        FilePath GetGlueExecutingFolder();

        bool RenameReferencedFileSave(ReferencedFileSave rfs, string newName);
    }
}
