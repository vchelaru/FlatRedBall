using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using OfficialPlugins.ErrorPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.ErrorPlugin.Logic
{
    public class ErrorCreateRemoveLogic : Singleton<ErrorCreateRemoveLogic>, IErrorReporter
    {
        #region Add Errors

        public static List<ErrorViewModel> GetMissingFileErrorViewModels()
        {
            List<ErrorViewModel> errorList = new List<ErrorViewModel>();

            var allFiles = GlueState.Self.GetAllReferencedFiles();

            foreach(var file in allFiles)
            {
                TryAddDirectMissingFileError(errorList, file);

                TryAddIndirectMissingFileErrors(errorList, file);
            }

            AddAllFileParseErrors(errorList);

            return errorList;
        }

        private static void AddAllFileParseErrors(List<ErrorViewModel> errorList)
        {
            foreach(var kvp in FileReferenceManager.Self.FilesWithFailedGetReferenceCalls)
            {
                FileParseErrorViewModel newError = new FileParseErrorViewModel(kvp.Key, kvp.Value);

            }
        }

        private static void TryAddDirectMissingFileError(List<ErrorViewModel> toReturn, ReferencedFileSave file)
        {
            var absolutePath = GlueCommands.Self.GetAbsoluteFileName(file);

            var exists = System.IO.File.Exists(absolutePath);
            if (!exists)
            {
                MissingFileErrorViewModel newError = new MissingFileErrorViewModel(file);

                toReturn.Add(newError);
            }
        }

        internal static void AddNewErrorsForFileReadError(FilePath filePath, ErrorListViewModel errorListViewModel)
        {
            AddFileParseErrors(filePath, errorListViewModel);
        }

        private static void TryAddIndirectMissingFileErrors(List<ErrorViewModel> errorList, ReferencedFileSave file)
        {

            List<FilePath> referenceStack = new List<FilePath>();

            var absoluteFile = GlueCommands.Self.GetAbsoluteFileName(file);
            referenceStack.Add(absoluteFile);

            TryAddIndirectMissingFileErrors(errorList, file, absoluteFile, referenceStack);
        }

        private static void TryAddIndirectMissingFileErrors(List<ErrorViewModel> errorList, ReferencedFileSave rootRfs, string absoluteFile, List<FilePath> referenceStack)
        {
            var referencedFiles = GlueCommands.Self.FileCommands.GetFilesReferencedBy(absoluteFile, EditorObjects.Parsing.TopLevelOrRecursive.TopLevel);

            foreach(var referencedFile in referencedFiles)
            {
                // make sure it's not already in the stack, to prevent circular references:
                var isAlreadyInStack = referenceStack.Any(item => item == referencedFile);

                if(!isAlreadyInStack)
                {
                    if(System.IO.File.Exists(referencedFile) == false)
                    {
                        var newError = new IndirectMissingFileErrorViewModel(rootRfs, referenceStack.ToList(), referencedFile);

                        errorList.Add(newError);
                    }
                    else
                    {
                        referenceStack.Add(referencedFile);

                        TryAddIndirectMissingFileErrors(errorList, rootRfs, referencedFile, referenceStack);

                        referenceStack.RemoveAt(referenceStack.Count - 1);
                    }
                }
            }

        }
        

        public static void AddNewErrorsForChangedFile(FilePath fileName, ErrorListViewModel errors)
        {
            var rfs = GlueCommands.Self.FileCommands.GetReferencedFile(fileName.Standardized);

            if(rfs != null)
            {
                AddNewDirectMissingFileErrors(fileName, errors, rfs);
            }

            AddNewIndirectMissingFileErrors(errors);

            AddFileParseErrors(fileName, errors);
        }

        private static void AddNewDirectMissingFileErrors(FilePath fileName, ErrorListViewModel errors, ReferencedFileSave rfs)
        {
            var directErrorsFound = new List<ErrorViewModel>();

            TryAddDirectMissingFileError(directErrorsFound, rfs);
            
            if(directErrorsFound.Any())
            {
                var existingErrors = errors.Errors
                    .Where(item => item is MissingFileErrorViewModel)
                    .Select(item => item as MissingFileErrorViewModel);

                foreach(MissingFileErrorViewModel newError in directErrorsFound)
                {
                    var alreadyExists = existingErrors.Any(item => item.AbsoluteMissingFile == newError.AbsoluteMissingFile);

                    if(!alreadyExists)
                    {
                        lock (GlueState.ErrorListSyncLock)
                        {
                            errors.Errors.Add(newError);
                        }
                    }
                }
            }
        }

        private static void AddNewIndirectMissingFileErrors(ErrorListViewModel errors)
        {
            // can only be an indirect error if the file can reference content
            // Update - but the changed file can be referenced too, even though
            // it itself may not reference content (like a png)
            //var canReferenceContent = FileHelper.DoesFileReferenceContent(fileName.Standardized);
            //if(canReferenceContent)


            // If a file changes, then we need to see if there are any indirect missing files caused by the change. 
            // The easiest way to do this is to perform a full scan and not add dupes:
            var indirectErrorsFound = new List<ErrorViewModel>();
            var allFiles = GlueState.Self.GetAllReferencedFiles();
            foreach (var file in allFiles)
            {
                TryAddIndirectMissingFileErrors(indirectErrorsFound, file);
            }

            // now add them:
            var existingIndirects = errors.Errors.Where(item => item is IndirectMissingFileErrorViewModel).Select(item => item as IndirectMissingFileErrorViewModel);
            foreach (var candidateToAdd in indirectErrorsFound)
            {
                var shouldAdd = existingIndirects.Any(item => item.Matches(candidateToAdd)) == false;
                if (shouldAdd)
                {
                    lock (GlueState.ErrorListSyncLock)
                    {
                        errors.Errors.Add(candidateToAdd);
                    }
                }
            }
        }

        private static void AddFileParseErrors(FilePath fileName, ErrorListViewModel errors)
        {
            var generalResponse = GlueCommands.Self.FileCommands.GetLastParseResponse(fileName);

            if(generalResponse.Succeeded == false)
            {
                var existingFileParseErrors = errors.Errors
                    .Where(item => item is FileParseErrorViewModel)
                    .Select(item => item as FileParseErrorViewModel)
                    .Where(item => item.FilePath == fileName && item.GeneralResponse.Message == generalResponse.Message);

                if(existingFileParseErrors.Count() == 0)
                {
                    // add it:
                    FileParseErrorViewModel newError = new FileParseErrorViewModel(fileName, generalResponse);
                    lock (GlueState.ErrorListSyncLock)
                    {
                        errors.Errors.Add(newError);
                    }
                }

            }
        }

        #endregion

        #region Remove

        internal static void RemoveFixedErrorsForChangedFile(FilePath filePath, ErrorListViewModel errorListViewModel)
        {
            for (int i = errorListViewModel.Errors.Count - 1; i > -1; i--)
            {
                var error = errorListViewModel.Errors[i];

                if (error.ReactsToFileChange(filePath) && error.GetIfIsFixed())
                {
                    lock (GlueState.ErrorListSyncLock)
                    {
                        errorListViewModel.Errors.RemoveAt(i);
                    }
                }
            }
        }

        internal static void RemoveFixedErrorsForRemovedRfs(ReferencedFileSave removedFile, ErrorListViewModel errorListViewModel)
        {
            FilePath filePath = GlueCommands.Self.GetAbsoluteFileName(removedFile);

            for (int i = errorListViewModel.Errors.Count - 1; i > -1; i--)
            {
                var error = errorListViewModel.Errors[i];

                if (error.ReactsToFileChange(filePath) && error.GetIfIsFixed())
                {
                    lock (GlueState.ErrorListSyncLock)
                    {
                        errorListViewModel.Errors.RemoveAt(i);
                    }
                }
            }
        }


        #endregion

        public ErrorViewModel[] GetAllErrors()
        {
            return GetMissingFileErrorViewModels().ToArray();
        }
    }
}
