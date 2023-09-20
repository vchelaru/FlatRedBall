using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using Npc.ViewModels;
using Npc.Managers;
using ToolsUtilities;
using System.Threading.Tasks;
using FRBDKUpdater;
using UpdaterWpf.Views;

namespace Npc
{
    public static class ProjectCreationHelper
    {
        #region Fields

        /// <summary>
        /// This list contains names which should not be used as project names.
        /// The reason is because the name can conflict with existing types or namespaces.
        /// </summary>
        static List<string> mReservedProjectNames = new List<string>()
        {
            "Game1",
            "FlatRedBall",
            "Microsoft",
            "System",
            "Microsoft.Xna",
            "SpriteEditor",
            "Camera",
            "SpriteManager",
            "ModelManager",
            "ShapeManager",
            "Sprite",
            "Scene",

            "new",
            "float",
            "int",
            "byte",
            "long",
            "double",
            "decimal",
            "if",
            "while",
            "do",
            "as",
            "is",
            "foreach",
            "for",
            "namespace",
            "class",
            "Action",
            "Func",
            "Task"
        };

        static char[] invalidCharacters = new char[] 
        { 
            '`', '~', '!', '@', '#', '$', '%', '^', 
            '&', '*', '(', ')', '-', '+', '=', ',',
            '[', '{', ']', '}', '\\', '|', '.', '<',
            '>', '/', '?'
        };

        #endregion

        static void ShowMessageBox(string message)
        {
            System.Windows.MessageBox.Show(message);
        }

        public static async Task<bool> MakeNewProject(NewProjectViewModel viewModel)
        {
            string stringToReplace = GetDefaultProjectNamespace(viewModel.SelectedProject);

            string projectZipUrl = GetFileToDownload(viewModel);

            var generalResponse = GeneralResponse.SuccessfulResponse;
            
            if (CommandLineManager.Self.OpenedBy != null && CommandLineManager.Self.OpenedBy.Equals("glue", StringComparison.OrdinalIgnoreCase))
            {
                var ppi = viewModel.SelectedProject;

                if (!ppi.SupportedInGlue)
                {
                    generalResponse.Succeeded = false;
                    ShowMessageBox("This project type is not supported in Glue.  You must launch the New Project Creator manually");
                }
            }

            string unpackDirectory = viewModel.FinalDirectory;

            if (generalResponse.Succeeded)
            {

                bool isFileNameValid = GetIfFileNameIsValid(viewModel, unpackDirectory);
                unpackDirectory = viewModel.FinalDirectory;

                if (!isFileNameValid)
                {
                    generalResponse.Succeeded = false;
                    generalResponse.Message = "Invalid file name";
                }
            }

            if (generalResponse.Succeeded)
            {

                bool shouldTryDownloading;
                var zipToUnpack = GetZipToUnpack(viewModel, projectZipUrl, out shouldTryDownloading);

                if (shouldTryDownloading)
                {
                    // Checks for a newer version and downloads it if necessary
                    generalResponse = DownloadFileSync(viewModel, zipToUnpack, projectZipUrl);

                    if (!generalResponse.Succeeded)
                    {
                        ShowMessageBox("Error downloading the file:\n" + generalResponse.Message);
                    }
                }


                if (generalResponse.Succeeded)
                {
                    try
                    {
                        Directory.CreateDirectory(unpackDirectory);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        ShowMessageBox("The program does not have permission to create a directory at\n\n" + unpackDirectory + "\n\nPlease run as administrator mode");
                        generalResponse.Succeeded = false;
                    }

                    if (generalResponse.Succeeded)
                    {
                        if (!File.Exists(zipToUnpack))
                        {
                            ShowMessageBox("Could not find the template file:\n" + zipToUnpack);
                        }


                        generalResponse.Succeeded = await UnzipManager.UnzipFile(zipToUnpack, unpackDirectory);
                    }

                    if (generalResponse.Succeeded)
                    {
                        RenameEverything(viewModel, stringToReplace, unpackDirectory);

                        GuidLogic.ReplaceGuids(unpackDirectory);

                        if (viewModel.OpenSlnFolderAfterCreation)
                        {
                            // according to ...
                            // https://stackoverflow.com/questions/35031856/access-is-denied-exception-when-using-process-start-to-open-folder
                            System.Diagnostics.Process.Start(Environment.GetEnvironmentVariable("WINDIR") + @"\explorer.exe", unpackDirectory);
                            //Process.Start(unpackDirectory);
                        }

                        System.Console.Out.WriteLine(unpackDirectory);

                    }
                }
            }

            return generalResponse.Succeeded;
        }

        
        private static void RenameEverything(NewProjectViewModel viewModel, string stringToReplace, string unpackDirectory)
        {
            var newProjectName = viewModel.ProjectName;

            if(stringToReplace != newProjectName)
            {
                RenameFiles(unpackDirectory, stringToReplace,
                    newProjectName);

                UpdateSolutionContents(unpackDirectory, stringToReplace,
                    newProjectName);
            }

            var newNamespace = viewModel.DifferentNamespace ?? viewModel.ProjectName;
            if(stringToReplace != newNamespace)
            {
                UpdateNamespaces(unpackDirectory, stringToReplace,
                    newNamespace);
            }

        }
        
        private static string GetZipToUnpack(NewProjectViewModel viewModel, string fileToDownload, out bool shouldTryDownloading)
        {
            bool checkOnline = viewModel.UseLocalCopy == false ;
            string zipToUnpack = null;

            if (!string.IsNullOrEmpty(fileToDownload))
            {
                zipToUnpack = FileManager.UserApplicationDataForThisApplication + FileManager.RemovePath(fileToDownload);
            }

            if (zipToUnpack == null)
            {
                // todo - handle this somehow?
                //string message = "There is no zip file online for this template.  What would you like to do?";
                //ShowErrorMessageBox(ref hasUserCancelled, ref zipToUnpack, message);

                //// null means user wants to use default file name
                //if (string.IsNullOrEmpty(zipToUnpack))
                //{
                //    zipToUnpack = FileManager.UserApplicationDataForThisApplication + viewModel.SelectedTemplate.BackingData.ZipName;
                //}

                checkOnline = false;
            }


            shouldTryDownloading = checkOnline ||
                !System.IO.File.Exists(zipToUnpack);

            return zipToUnpack;
        }
        /*
        private static void ShowErrorMessageBox(ref bool hasUserCancelled, ref string zipToUnpack, string message)
        {
            // todo - eventually make an interface for this, inject it
            MultiButtonMessageBox mbmb = new MultiButtonMessageBox();
            mbmb.MessageText = message;
            mbmb.AddButton("Look for the file in the default location", System.Windows.Forms.DialogResult.Yes);
            mbmb.AddButton("Select a .zip file to use", System.Windows.Forms.DialogResult.OK);
            mbmb.AddButton("Nothing - don't create a project", System.Windows.Forms.DialogResult.Cancel);
            var dialogResult = mbmb.ShowDialog();

            if (dialogResult == System.Windows.Forms.DialogResult.Yes)
            {
                // do nothing, it'll just look in the default location....
            }
            else if (dialogResult == System.Windows.Forms.DialogResult.OK)
            {
                System.Windows.Forms.OpenFileDialog fileDialog = new System.Windows.Forms.OpenFileDialog();
                fileDialog.InitialDirectory = "c:\\";
                fileDialog.Filter = "Zipped FRB template (*.zip)|*.zip";
                fileDialog.RestoreDirectory = true;

                if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    zipToUnpack = fileDialog.FileName;
                }
            }
            else if (dialogResult == System.Windows.Forms.DialogResult.Cancel)
            {
                hasUserCancelled = true;
            }
        }
*/
        private static bool GetIfFileNameIsValid(NewProjectViewModel viewModel, string unpackDirectory)
        {
            string whyIsInvalid = null;

            bool isFileNameValid = true;
            #region Check for spaces if creating a directory for the project

            if (viewModel.IsCreateProjectDirectoryChecked)
            {
                if (viewModel.ProjectName.Contains(" "))
                {
                    whyIsInvalid = "Project names cannot contain spaces.";
                }
            }

            #endregion

            // I think we can be more lenient here, let's allow empty directories so users can put new projects in github folders.
            if (string.IsNullOrEmpty(whyIsInvalid) && Directory.Exists(unpackDirectory))
            {
                bool hasFiles = Directory.GetFiles(unpackDirectory).Any();

                if(hasFiles)
                {
                    whyIsInvalid = "The directory " + unpackDirectory + " is not empty";
                }
            }

            if(!string.IsNullOrEmpty(whyIsInvalid))
            {
                ShowMessageBox(whyIsInvalid);
            }

            isFileNameValid = string.IsNullOrEmpty(whyIsInvalid);
            return isFileNameValid;
        }

        private static GeneralResponse DownloadFileSync(NewProjectViewModel viewModel, string zipToUnpack, string fileToDownoad)
        {
            var urs = new UpdaterRuntimeSettings();
            urs.FileToDownload = fileToDownoad;
            urs.FormTitle = "Downloading " + viewModel.SelectedProject.FriendlyName;

            //if (string.IsNullOrEmpty(zipToUnpack))
            //{
            //    throw new Exception("The zipToUnpack argument is null - it shouldn't be");
            //}

            urs.LocationToSaveFile = zipToUnpack;

            string whereToSaveSettings =
                FileManager.UserApplicationDataForThisApplication + "DownloadInformation." + UpdaterRuntimeSettings.RuntimeSettingsExtension;


            //ProcessStartInfo psi = new ProcessStartInfo();
            //psi.FileName = resultingLocation;

            //// The username for the user may have a space in it
            //// so we need to have quotes around the path

            //psi.Arguments = "\"" + whereToSaveSettings + "\"";


            //Process process = Process.Start(psi);

            //while (!process.HasExited)
            //{
            //    System.Threading.Thread.Sleep(200);
            //}
            //bool succeeded = process.ExitCode == 0;


            //return succeeded;

            var window = new UpdaterWpf.Views.MainWindow(whereToSaveSettings, urs);
            window.CancelButtonVisibility = viewModel.IsCancelButtonVisible
                ? System.Windows.Visibility.Visible
                : System.Windows.Visibility.Collapsed;

            window.Owner = viewModel.owner;
            var result = window.ShowDialog();

            if(result == null)
            {
                return GeneralResponse.UnsuccessfulWith("Download Cancelled");
            }
            else
            {
                return window.GeneralResponse ?? GeneralResponse.UnsuccessfulWith("Download Cancelled");
            }
        }

        private static string GetFileToDownload(NewProjectViewModel viewModel)
        {

            PlatformProjectInfo foundInstance = viewModel.SelectedProject;

            if (foundInstance == null)
            {
                throw new NotImplementedException("You must first select a template");
            }
            else
            {
                return foundInstance.Url;
            }
        }
        
        /// <summary>
        /// Renames all contained files to match the project name.  For example if the project
        /// is called RaceCar, then this makes RaceCar.sln, RaceCar.csproj, and so on.
        /// </summary>
        /// <param name="unpackDirectory">The directory containing all of the files to rename.</param>
        /// <param name="stringToReplace">What to replace - usually this is the platform name.</param>
        /// <param name="stringToReplaceWith">What to replace with - this is the project name.</param>
        private static void RenameFiles(string unpackDirectory, string stringToReplace, string stringToReplaceWith)
        {
            List<string> filesToReplace = FileManager.GetAllFilesInDirectory(
                unpackDirectory, "csproj");

            filesToReplace.AddRange(FileManager.GetAllFilesInDirectory(
                unpackDirectory, "sln"));


            filesToReplace.AddRange(FileManager.GetAllFilesInDirectory(
                unpackDirectory, "suo"));

            filesToReplace.AddRange(FileManager.GetAllFilesInDirectory(
                unpackDirectory, "html"));

            filesToReplace.AddRange(FileManager.GetAllFilesInDirectory(
                unpackDirectory, "aspx"));

            filesToReplace.AddRange(FileManager.GetAllFilesInDirectory(
                unpackDirectory, "user"));

            filesToReplace.AddRange(FileManager.GetAllFilesInDirectory(
                unpackDirectory, "apk"));

            filesToReplace.AddRange(FileManager.GetAllFilesInDirectory(
                unpackDirectory, "java"));

            filesToReplace.AddRange(FileManager.GetAllFilesInDirectory(
                unpackDirectory, "contentproj"));

            filesToReplace.AddRange(FileManager.GetAllFilesInDirectory(
                unpackDirectory, "glux"));

            filesToReplace.AddRange(FileManager.GetAllFilesInDirectory(
                unpackDirectory, "gluj"));

            filesToReplace.AddRange(FileManager.GetAllFilesInDirectory(
                unpackDirectory, "pfx"));


            foreach (string fileName in filesToReplace)
            {
                if (fileName.Contains(stringToReplace))
                {
                    string directory = FileManager.GetDirectory(fileName);
                    string fileWithoutPath = FileManager.RemovePath(fileName);

                    fileWithoutPath = fileWithoutPath.Replace(stringToReplace, stringToReplaceWith);

                    TryMultipleTimes(() => File.Move(fileName, directory + fileWithoutPath), 5);
                }

            }

            bool shouldRepeat = true;

            List<string> directoriesAlreadyRenamed = new List<string>();

            while (shouldRepeat)
            {
                string[] directories = Directory.GetDirectories(unpackDirectory, stringToReplace + "*", SearchOption.AllDirectories);

                int i = 0;

                for (i = 0; i < directories.Length; i++)
                {
                    string fileName = directories[i];

                    if (!directoriesAlreadyRenamed.Contains(fileName) && fileName.Contains(stringToReplace))
                    {
                        int lastIndexOfWhatToReplace = fileName.LastIndexOf(stringToReplace);

                        string beforeLastIndex = fileName.Substring(0, lastIndexOfWhatToReplace);

                        string after = fileName.Substring(lastIndexOfWhatToReplace, fileName.Length - lastIndexOfWhatToReplace);
                        after = after.Replace(stringToReplace, stringToReplaceWith);

                        string targetDirectory = beforeLastIndex + after;

                        TryMultipleTimes( () => Directory.Move(fileName, targetDirectory), 5);

                        shouldRepeat = true;

                        directoriesAlreadyRenamed.Add(targetDirectory);

                        break;
                    }
                }

                if (i >= directories.Length)
                {
                    shouldRepeat = false;
                }
            }

        }
        private static void UpdateSolutionContents(string unpackDirectory, string stringToReplace, string stringToReplaceWith)
        {
            List<string> filesToFix = FileManager.GetAllFilesInDirectory(
                unpackDirectory, "sln");


            foreach (string fileName in filesToFix)
            {
                string contents = FileManager.FromFileText(fileName);

                contents = contents.Replace(stringToReplace, stringToReplaceWith);

                FileManager.SaveText(contents, fileName);
            }
            foreach (string fileName in filesToFix)
            {
                EncodeSLNFiles(fileName);
            }
        }
        
        /*
        private static string getProjectFromSln(string slnLocation)
        {
            string contentsOfSln = FileManager.FromFileText(slnLocation);
            int startIndex = contentsOfSln.IndexOf("Project");
            int endIndex = contentsOfSln.IndexOf("EndProject");

            string projectInformation = contentsOfSln.Substring(startIndex, endIndex - startIndex + "EndProject".Length);

            int indexOfCsProj = contentsOfSln.IndexOf(".csproj");

            int indexOfQuoteBefore = contentsOfSln.LastIndexOf('"', indexOfCsProj);
            int indexofQuoteAfter = contentsOfSln.IndexOf('"', indexOfCsProj);



            string csproj = contentsOfSln.Substring(indexOfQuoteBefore + 1, indexofQuoteAfter - indexOfQuoteBefore - 1);

            string solutionDirectory = FileManager.GetDirectory(slnLocation).Replace('/', '\\');

            string absoluteCsProj = solutionDirectory + csproj;

            projectInformation = projectInformation.Replace(csproj, absoluteCsProj);

            return projectInformation;
 
        }
        */
        static void EncodeSLNFiles(string FileName)
        {
            string fileContents;
            StreamReader streamRead = new StreamReader(FileName);
            fileContents = streamRead.ReadToEnd();
            streamRead.Close();
            StreamWriter streamWrite = new StreamWriter(FileName, false, Encoding.UTF8);
            streamWrite.Write(fileContents);
            streamWrite.Close();
        }
        private static void UpdateNamespaces(string unpackDirectory, string stringToReplace, string stringToReplaceWith)
        {
            // simple replacements:
            List<string> 
                filesToFix = FileManager.GetAllFilesInDirectory(unpackDirectory, "cs");

            filesToFix.AddRange(FileManager.GetAllFilesInDirectory(unpackDirectory, "xaml"));
            filesToFix.AddRange(FileManager.GetAllFilesInDirectory(unpackDirectory, "aspx"));
            filesToFix.AddRange(FileManager.GetAllFilesInDirectory(unpackDirectory, "html"));
            filesToFix.AddRange(FileManager.GetAllFilesInDirectory(unpackDirectory, "user"));
            filesToFix.AddRange(FileManager.GetAllFilesInDirectory(unpackDirectory, "appxmanifest"));


            foreach (string fileName in filesToFix)
            {
                string contents = FileManager.FromFileText(fileName);

                contents = contents.Replace(stringToReplace, stringToReplaceWith);

                FileManager.SaveText(contents, fileName);
            }

            filesToFix.Clear();

            filesToFix = FileManager.GetAllFilesInDirectory(unpackDirectory, "csv");

            foreach (string fileName in filesToFix)
            {
                string contents = FileManager.FromFileText(fileName);
                // to catch namespaces:
                contents = contents.Replace(stringToReplace + ".", stringToReplaceWith + ".");

                FileManager.SaveText(contents, fileName);
            }

            filesToFix.Clear();

            filesToFix.AddRange(FileManager.GetAllFilesInDirectory(unpackDirectory, "csproj"));

            // We want to be careful doing a pure copy/paste 
            // replace in .csproj files, because .csproj files
            // can reference .dlls which have names that contain
            // the old namespace
            foreach (string fileName in filesToFix)
            {
                string contents = FileManager.FromFileText(fileName);
                string namespaceLine =
                    "<RootNamespace>" + stringToReplace + "</RootNamespace>";

                string namespaceLineReplacement =
                    "<RootNamespace>" + stringToReplaceWith + "</RootNamespace>";

                contents = contents.Replace(namespaceLine, namespaceLineReplacement);

                string originalStartup = "<StartupObject>" + stringToReplace + ".Program</StartupObject>";
                string replacementStartup = "<StartupObject>" + stringToReplaceWith + ".Program</StartupObject>";
                contents = contents.Replace(originalStartup, replacementStartup);

                string originalAssemblyName = $"<AssemblyName>{stringToReplace}</AssemblyName>";
                string newAssemblyName = $"<AssemblyName>{stringToReplaceWith}</AssemblyName>";
                contents = contents.Replace(originalAssemblyName, newAssemblyName);

                string originalProjectReference = $@"<ProjectReference Include=""..\{stringToReplace}Content\{stringToReplace}Content.contentproj"">";
                string newProjectReference = $@"<ProjectReference Include=""..\{stringToReplaceWith}Content\{stringToReplaceWith}Content.contentproj"">";
                contents = contents.Replace(originalProjectReference, newProjectReference);

                System.IO.File.WriteAllText(fileName, contents, Encoding.UTF8);
            }

            filesToFix.Clear();
            filesToFix.AddRange(FileManager.GetAllFilesInDirectory(unpackDirectory, "contentproj"));
            foreach (string fileName in filesToFix)
            {
                string contents = FileManager.FromFileText(fileName);

                string whatToReplace = $"\\{stringToReplace}\\Libraries\\";
                string whatToReplaceWith = $"\\{stringToReplaceWith}\\Libraries\\";

                contents = contents.Replace(whatToReplace, whatToReplaceWith);

                System.IO.File.WriteAllText(fileName, contents, Encoding.UTF8);
            }


            filesToFix.Clear();
            filesToFix.AddRange(FileManager.GetAllFilesInDirectory(unpackDirectory, "glux"));
            filesToFix.AddRange(FileManager.GetAllFilesInDirectory(unpackDirectory, "gluj"));

            foreach (string fileName in filesToFix)
            {
                string contents = FileManager.FromFileText(fileName);

                string whatToSearchFor = stringToReplace + ".";
                string replacement = stringToReplaceWith + ".";

                contents = contents.Replace(whatToSearchFor, replacement);

                FileManager.SaveText(contents, fileName);
            }
        }
        
        /*
        private static void UpdateJavaFiles(string unpackDirectory, string stringToReplace, string stringToReplaceWith)
        {
            List<string> filesToFix = FileManager.GetAllFilesInDirectory(
                unpackDirectory, "java");
            filesToFix.AddRange(FileManager.GetAllFilesInDirectory(
                unpackDirectory, "xml"));
            filesToFix.AddRange(FileManager.GetAllFilesInDirectory(
                unpackDirectory, "project"));
            filesToFix.AddRange(FileManager.GetAllFilesInDirectory(
                unpackDirectory, "contentproj"));

            foreach (string fileName in filesToFix)
            {
                string contents = FileManager.FromFileText(fileName);
                contents = contents.Replace(stringToReplace, stringToReplaceWith);
                FileManager.SaveText(contents, fileName);
            }
        }

        */
        /// <summary>
        /// Returns with default directory for templates (AppData) 
        /// </summary>
        /// <param name="zipToUnpack"></param>
        /// <param name="stringToReplace"></param>
        public static string GetDefaultProjectNamespace(PlatformProjectInfo project)
        {
            string defaultProjectNamespace = String.Empty;
            GetDefaultZipLocationAndStringToReplace(project,
                  FileManager.UserApplicationDataForThisApplication, out string _, out defaultProjectNamespace);

            return defaultProjectNamespace;
        }
        public static void GetDefaultZipLocationAndStringToReplace(PlatformProjectInfo project, string templateLocation, out string zipToUnpack, out string stringToReplace)
        {
            if(project == null)
            {
                throw new ArgumentNullException("project");
            }
            zipToUnpack = "";

            stringToReplace = "";
                        
            if (!Directory.Exists(templateLocation))
            {
                var targetDirectory = new DirectoryInfo(templateLocation);
                var parent = targetDirectory.Parent;
                parent.CreateSubdirectory(targetDirectory.Name);
                //new DirectoryInfo(Application.persistentDataPath).CreateSubdirectory(...)
                //Directory.CreateDirectory(templateLocation);
            }


            zipToUnpack = templateLocation + project.ZipName;
            stringToReplace = project.Namespace;
        }


        internal static string GetWhyProjectNameIsntValid(string projectName)
        {
            if (string.IsNullOrEmpty(projectName))
            {
                return "Project name can't be blank";
            }

            if (char.IsDigit(projectName[0]))
            {
                return "Project names can't start with numbers.";
            }

            if (projectName.Contains(' '))
            {
                return "Project name can't contain spaces.";
            }


            int indexOfInvalid = projectName.IndexOfAny(invalidCharacters);
            if (indexOfInvalid != -1)
            {
                return "Project name can't contain the character " + projectName[indexOfInvalid];
            }

            if (mReservedProjectNames.Contains(projectName))
            {
                return "The name " + projectName + " is reserved.  Please pick another name";
            }
            
            return null;
            

        }
        

        private static void TryMultipleTimes(Action action, int numberOfTimesToTry)
        {
            const int msSleep = 200;

            int failureCount = 0;

            while(failureCount < numberOfTimesToTry)
            {
                try
                {
                    action();
                    break;
                }

                
                catch(Exception e)
                {
                    failureCount++;
                    System.Threading.Thread.Sleep(msSleep);
                    if(failureCount >= numberOfTimesToTry)
                    {
                        throw e;
                    }
                }
            }
        }
    }
}
