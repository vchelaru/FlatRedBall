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
using GeneralResponse = ToolsUtilities.GeneralResponse;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    public interface IFileCommands
    {
        GeneralResponse GetLastParseResponse(FilePath file);

        IEnumerable<FilePath> GetFilesReferencedBy    (string absoluteName, EditorObjects.Parsing.TopLevelOrRecursive topLevelOrRecursive);
        IEnumerable<FilePath> GetFilePathsReferencedBy(FilePath filePath, EditorObjects.Parsing.TopLevelOrRecursive topLevelOrRecursive);

        IEnumerable<FilePath> GetFilesReferencedBy(ReferencedFileSave file, EditorObjects.Parsing.TopLevelOrRecursive topLevelOrRecursive);

        IEnumerable<FilePath> GetAllFilesNeededOnDisk();

        List<FilePath> GetAllReferencedFileNames();

        /// <summary>
        /// Returns all FilePaths referenced by ReferencedFileSaves, both directly and indirectly. This only includes source file, not built files.
        /// For built files, see GetAllFilesNeededOnDisk.
        /// </summary>
        /// <returns></returns>
        List<FilePath> GetAllReferencedFilePaths();

        List<FilePath> GetAllReferencedFileNames(TopLevelOrRecursive topLevelOrRecursive);

        void ClearFileCache(FilePath absoluteName);

        FilePath GetJsonFilePath(GlueElement element);


        string GetContentFolder(GlueElement element);
        FilePath GetGlobalContentFolder();

        void IgnoreNextChangeOnFile(string absoluteFileName);
        void IgnoreNextChangeOnFile(FilePath filePath);
        void IgnoreChangeOnFileUntil(FilePath filePath, DateTimeOffset expiration);

        string GetFullFileName(ReferencedFileSave rfs);
        FilePath GetFilePath(ReferencedFileSave rfs);
        ReferencedFileSave GetReferencedFile(FilePath filePath);
        List<ReferencedFileSave> GetReferencedFiles(FilePath filePath);

        /// <summary>
        /// Returns all ReferencedFileSaves that use the argument file as their source - in other words,
        /// the ReferencedFileSave converts the source to a destination file, and the destination file is
        /// ultimately used in game. Source files are used to build destination files, such as ODS->CSV.
        /// </summary>
        List<ReferencedFileSave> GetReferencedFilesUsingSourceFile(FilePath filePath);


        FilePath GetCustomCodeFilePath(GlueElement glueElement);

        FilePath GetGlueExecutingFolder();

        Task PasteFolder(FilePath sourceFolder, FilePath destinationFolder);


        bool RenameReferencedFileSave(ReferencedFileSave rfs, string newName);

        /// <summary>
        /// Returns whether the argument FilePath is a content file (such as a PNG)
        /// </summary>
        /// <param name="filePath">The argument filepath</param>
        /// <returns>Whether the filepath is considered content.</returns>
        bool IsContent(FilePath filePath);
        void ViewInExplorer(FilePath filePath);

        void Open(FilePath filePath);
        void OpenReferencedFileInDefaultProgram(ReferencedFileSave currentReferencedFileSave);
        void OpenFileInDefaultProgram(string fileName, string OpensWith = null);

        public List<string> GumFileExtensions { get; }
        public string GetGumExeFilePath();

        void SaveIfDiffers(FilePath filePath, string contents, bool ignoreNextChange = false);
    }
}
