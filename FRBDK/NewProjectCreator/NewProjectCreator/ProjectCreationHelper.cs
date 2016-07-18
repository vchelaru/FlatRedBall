using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Ionic.Zip;
//using FlatRedBall.IO;
using System.Diagnostics;
//using NewProjectCreator.Remote;
//using FlatRedBall.Utilities;
//using FRBDKUpdater;
//using FlatRedBall.IO.Csv;
using FlatRedBall.Glue.Controls;
//using NewProjectCreator.Managers;
//using NewProjectCreator.Logic;
using System.Windows.Forms;
using NewProjectCreator.ViewModels;
using FlatRedBall.IO;
using FlatRedBall.IO.Csv;
using FlatRedBall.Utilities;
using FRBDKUpdater;
using NewProjectCreator.Managers;

namespace NewProjectCreator
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
            "Scene"
        };

        static char[] invalidCharacters = new char[] 
        { 
            '`', '~', '!', '@', '#', '$', '%', '^', 
            '&', '*', '(', ')', '-', '+', '=', ',',
            '[', '{', ']', '}', '\\', '|', '.', '<',
            '>', '/', '?'
        };

        #endregion

        public static bool MakeNewProject(NewProjectViewModel viewModel)
        {


            string stringToReplace;
            string zipToUnpack;
            GetDefaultZipLocationAndStringToReplace(viewModel.ProjectType, out zipToUnpack, out stringToReplace);
            string fileToDownload = GetFileToDownload(viewModel);

            bool succeeded = true;

			#if !MAC
            if (NewProjectCreator.Managers.CommandLineManager.Self.OpenedBy != null && NewProjectCreator.Managers.CommandLineManager.Self.OpenedBy.ToLower() == "glue")
            {
                PlatformProjectInfo ppi = viewModel.ProjectType;

                if (!ppi.SupportedInGlue)
                {
                    succeeded = false;
                    MessageBox.Show("This project type is not supported in Glue.  You must launch the New Project Creator manually");
                }
            }
			#endif

            if (succeeded)
            {
                string unpackDirectory = viewModel.ProjectLocation;

                bool isFileNameValid = GetIfFileNameIsValid(viewModel, ref unpackDirectory);

                if(!isFileNameValid)
                {
                    succeeded = false;
                }
                else
                {
                    bool hasUserCancelled = false;

                    bool shouldTryDownloading;
                    zipToUnpack = GetZipToUnpack(viewModel, fileToDownload, ref hasUserCancelled, out shouldTryDownloading);

                    if(shouldTryDownloading)
                    {
                        // Checks for a newer version and downloads it if necessary
                        bool downloadSucceeded = DownloadFileSync(viewModel, zipToUnpack, fileToDownload);

                        if (!downloadSucceeded)
                        {
                            ShowErrorMessageBox(ref hasUserCancelled, ref zipToUnpack, "Error downloading the file.  What would you like to do?");
                        }
                    }


                    if (!hasUserCancelled)
                    {
                        try
                        {
                            Directory.CreateDirectory(unpackDirectory);
                        }
                        catch (UnauthorizedAccessException)
                        {
                            MessageBox.Show("The program does not have permission to create a directory at\n\n" + unpackDirectory + "\n\nPlease run as administrator mode");
                            succeeded = false;
                        }

                        if (succeeded)
                        {
                            if (!File.Exists(zipToUnpack))
                            {
                                System.Windows.Forms.MessageBox.Show("Could not find the template file:\n" + zipToUnpack);
                            }


                            succeeded = UnzipManager.UnzipFile(zipToUnpack, unpackDirectory);
                        }

                        if (succeeded)
                        {
                            RenameEverything(viewModel, stringToReplace, unpackDirectory);

                            CreateGuidInAssemblyInfo(unpackDirectory);

                            if (viewModel.OpenSlnFolderAfterCreation)
                            {
                                Process.Start(unpackDirectory);
                            }

                            System.Console.Out.WriteLine(unpackDirectory);

                        }
                    }
                }
            }

            return succeeded;
        }

        private static void RenameEverything(NewProjectViewModel viewModel, string stringToReplace, string unpackDirectory)
        {
            RenameFiles(unpackDirectory, stringToReplace,
                viewModel.ProjectName);

            UpdateSolutionContents(unpackDirectory, stringToReplace,
                viewModel.ProjectName);

            // UpdateProjects does a simple "Replace" call, which should
            // happen before we do the stricter UpdateNamespace call below.
            // Otherwise, if the user changes the project name to something that contains
            // the original name (like "StarBlaster"), then UpdateProject will result in the
            // namespace being incorrect.
            UpdateProjects(unpackDirectory, stringToReplace,
                viewModel.DifferentNamespace);

            UpdateNamespaces(unpackDirectory, stringToReplace,
                viewModel.DifferentNamespace);

        }

        private static string GetZipToUnpack(NewProjectViewModel viewModel, string fileToDownload, ref bool hasUserCancelled, out bool shouldTryDownloading)
        {
            bool checkOnline = viewModel.CheckForNewVersions;
            string zipToUnpack = null;
            shouldTryDownloading = false;
            if (!string.IsNullOrEmpty(fileToDownload))
            {
                zipToUnpack = FileManager.UserApplicationDataForThisApplication + FileManager.RemovePath(fileToDownload);
            }

            if (zipToUnpack == null)
            {
                string message = "There is no zip file online for this template.  What would you like to do?";
                ShowErrorMessageBox(ref hasUserCancelled, ref zipToUnpack, message);
                checkOnline = false;
            }


            if (checkOnline)
            {
                if (string.IsNullOrEmpty(fileToDownload))
                {
                    string message = "Couldn't find this project online.  What would you like to do?";

                    ShowErrorMessageBox(ref hasUserCancelled, ref zipToUnpack, message);
                }
                else
                {
                    shouldTryDownloading = true;
                }
            }
            return zipToUnpack;
        }

        private static void ShowErrorMessageBox(ref bool hasUserCancelled, ref string zipToUnpack, string message)
        {
            MultiButtonMessageBox mbmb = new MultiButtonMessageBox();
            mbmb.MessageText = message;
            mbmb.AddButton("Look for the file in the default location", DialogResult.Yes);
            mbmb.AddButton("Select a .zip file to use", DialogResult.OK);
            mbmb.AddButton("Nothing - don't create a project", DialogResult.Cancel);
            var dialogResult = mbmb.ShowDialog();

            if (dialogResult == DialogResult.Yes)
            {
                // do nothing, it'll just look in the default location....
            }
            else if (dialogResult == DialogResult.OK)
            {
                OpenFileDialog fileDialog = new OpenFileDialog();
                fileDialog.InitialDirectory = "c:\\";
                fileDialog.Filter = "Zipped FRB template (*.zip)|*.zip";
                fileDialog.RestoreDirectory = true;

                if (fileDialog.ShowDialog() == DialogResult.OK)
                {
                    zipToUnpack = fileDialog.FileName;
                }
            }
            else if (dialogResult == DialogResult.Cancel)
            {
                hasUserCancelled = true;
            }
        }

        private static bool GetIfFileNameIsValid(NewProjectViewModel viewModel, ref string unpackDirectory)
        {
            bool isFileNameValid = true;
            #region Check for spaces if creating a directory for the project

            if (viewModel.CreateProjectDirectory)
            {
                if (viewModel.ProjectName.Contains(" "))
                {
                    System.Windows.Forms.MessageBox.Show("Project names cannot contain spaces.");
                    isFileNameValid = false;
                }


                unpackDirectory = viewModel.CombinedProjectDirectory;
            }

            #endregion


            if (Directory.Exists(unpackDirectory))
            {
                MessageBox.Show("The directory " + unpackDirectory + " already exists");
                isFileNameValid = false;
            }
            return isFileNameValid;
        }


        private static bool DownloadFileSync(NewProjectViewModel viewModel, string zipToUnpack, string fileToDownoad)
        {
            EmbeddedExecutableExtractor eee = EmbeddedExecutableExtractor.Self;

            eee.ExtractFile("FlatRedBall.Tools.dll");
            eee.ExtractFile("Ionic.Zip.dll");
            eee.ExtractFile("Ionic.Zlib.dll");
            string resultingLocation = eee.ExtractFile("FRBDKUpdater.exe");


            UpdaterRuntimeSettings urs = new UpdaterRuntimeSettings();
            urs.FileToDownload = fileToDownoad;
            urs.FormTitle = "Downloading " + viewModel.ProjectType.FriendlyName;

            if (string.IsNullOrEmpty(zipToUnpack))
            {
                throw new Exception("The zipToUnpack argument is null - it shouldn't be");
            }

            urs.LocationToSaveFile = zipToUnpack;

            string whereToSaveSettings =
                FileManager.UserApplicationDataForThisApplication + "DownloadInformation." + UpdaterRuntimeSettings.RuntimeSettingsExtension;

            urs.Save(whereToSaveSettings);

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = resultingLocation;

            // The username for the user may have a space in it
            // so we need to have quotes around the path

            psi.Arguments = "\"" + whereToSaveSettings + "\"";


            Process process = Process.Start(psi);

            while (!process.HasExited)
            {
                System.Threading.Thread.Sleep(200);
            }
            bool succeeded = process.ExitCode == 0;


            return succeeded;
        }

        private static string GetFileToDownload(NewProjectViewModel viewModel)
        {

            PlatformProjectInfo foundInstance = viewModel.ProjectType;

            if (foundInstance == null)
            {
                throw new NotImplementedException("You must first select a template");
            }
            else
            {
                return foundInstance.Url;
            }
        }

        private static void CreateGuidInAssemblyInfo(string unpackDirectory)
        {

            List<string> stringList = FileManager.GetAllFilesInDirectory(unpackDirectory, "cs");

            foreach (string s in stringList)
            {
                if (s.ToLower().Contains("assemblyinfo.cs"))
                {
                    string contents = FileManager.FromFileText(s);

                    string newGuid = Guid.NewGuid().ToString();

                    string newLine = "[assembly: Guid(\"" + newGuid + "\")]";

                    StringFunctions.ReplaceLine(
                        ref contents, "[assembly: Guid(", newLine);

                    FileManager.SaveText(contents, s);
                }

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
                unpackDirectory, "pfx"));


            foreach (string fileName in filesToReplace)
            {
                if (fileName.Contains(stringToReplace))
                {
                    string directory = FileManager.GetDirectory(fileName);
                    string fileWithoutPath = FileManager.RemovePath(fileName);

                    fileWithoutPath = fileWithoutPath.Replace(stringToReplace, stringToReplaceWith);

                    File.Move(fileName, directory + fileWithoutPath);
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

                        Directory.Move(fileName, targetDirectory);

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
            List<string> filesToFix = FileManager.GetAllFilesInDirectory(
                unpackDirectory, "cs");

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
            filesToFix.AddRange(FileManager.GetAllFilesInDirectory(unpackDirectory, "csproj"));

            foreach (string fileName in filesToFix)
            {
                string contents = FileManager.FromFileText(fileName);
                string namespaceLine =
                    "<RootNamespace>" + stringToReplace + "</RootNamespace>";

                string namespaceLineReplacement =
                    "<RootNamespace>" + stringToReplaceWith + "</RootNamespace>";

                contents = contents.Replace(namespaceLine, namespaceLineReplacement);

                string original = "<StartupObject>" + stringToReplace + ".Program</StartupObject>";
                string replacement = "<StartupObject>" + stringToReplaceWith + ".Program</StartupObject>";
                contents = contents.Replace(original, replacement);

                FileManager.SaveText(contents, fileName);
            }

            filesToFix.Clear();
            filesToFix.AddRange(FileManager.GetAllFilesInDirectory(unpackDirectory, "glux"));

            foreach(string fileName in filesToFix)
            {
                string contents = FileManager.FromFileText(fileName);

                string whatToSearchFor = stringToReplace + ".";
                string replacement = stringToReplaceWith + ".";

                contents = contents.Replace(whatToSearchFor, replacement);

                FileManager.SaveText(contents, fileName);
            }
        }

        private static void UpdateProjects(string unpackDirectory, string stringToReplace, string stringToReplaceWith)
        {
            List<string> filesToFix = FileManager.GetAllFilesInDirectory(
                unpackDirectory, "csproj");

            filesToFix.AddRange(FileManager.GetAllFilesInDirectory(unpackDirectory, "contentproj"));

            foreach (string fileName in filesToFix)
            {
                string contents = FileManager.FromFileText(fileName);

                contents = contents.Replace(stringToReplace, stringToReplaceWith);

                FileManager.SaveText(contents, fileName);
            }
        }

        private static void AddEngineProjectsToProject(PlatformProjectInfo projectType, ref string contents)
        {
            
            #region Switch based off of the HighlightedTemplate

            

            string projectGUID = "";
            string projectPath = "";
            bool XNA4 = false;

            switch (projectType.FriendlyName)
            {
                case "FlatRedBall XNA 3.1 (PC)":
                    projectPath = @"N:\FlatRedBallXNA\FlatRedBall\FlatRedBall.csproj";
                    projectGUID = GetGUID(projectPath);
                    break;
                case "FlatRedBall XNA 4.0 (PC)":
                    projectPath = @"N:\FlatRedBallXNA\FlatRedBall\FlatRedBallXna4.csproj";
                    projectGUID = GetGUID(projectPath);
                    XNA4 = true;
                    break;
                case "FlatRedBall XNA 3.1 (Xbox 360)":
                    projectPath = @"N:\FlatRedBallXNA\FlatRedBall\FlatRedBallXbox360.csproj";
                    projectGUID = GetGUID(projectPath);
                    break;
                case "FlatRedBall XNA 4.0 (Xbox 360)":
                    projectPath = @"N:\FlatRedBallXNA\FlatRedBall\FlatRedBallXna4_360.csproj";
                    projectGUID = GetGUID(projectPath);
                    XNA4 = true;
                    break;
                case "FlatSilverBall (Browser)":
                    projectPath = @"N:\FlatSilverBall\FlatSilverBall\FlatRedBall.csproj";
                    projectGUID = GetGUID(projectPath);
                    break;
                case "FlatRedBall MDX (PC)":
                    projectPath = @"N:\FlatRedBallMDX\FRB.csproj";
                    projectGUID = GetGUID(projectPath);
                    break;
                case "FlatRedBall WindowsPhone (Phone)":
                    projectPath = @"N:\FlatRedBallXNA\FlatRedBall\FlatRedBallWindowsPhone.csproj";
                    projectGUID = GetGUID(projectPath);
                    XNA4 = true;
                    break;

                case "FlatRedBall Android (Phone)":
                    // do nothing for now!
                    break;
            }
            /*
            <ItemGroup>
                <ProjectReference Include="N:\FlatRedBallXNA\FlatRedBall\FlatRedBall.csproj">
                  <Project>{E1CB7D7B-E2EC-4DEB-92E2-6EF0B76F40F0}</Project>
                  <Name>FlatRedBall</Name>
                </ProjectReference>N
            </ItemGroup>
             */
            int referenceStartIndex = contents.IndexOf("<Reference Include=\"FlatRedBall,");

            // This could have something like this: <Reference Include="FlatRedBall">
            if (referenceStartIndex == -1)
            {
                referenceStartIndex = contents.IndexOf("<Reference Include=\"FlatRedBall");
            }

            int referenceEndIndex;


            int nextBackSlashReference = contents.IndexOf("</Reference>", referenceStartIndex);
            int nextBackSlashClose = referenceEndIndex = contents.IndexOf("/>", referenceStartIndex);

            if (nextBackSlashReference != -1 && nextBackSlashReference < nextBackSlashClose)
            {
                referenceEndIndex = contents.IndexOf("</Reference>", referenceStartIndex) + "</Reference>".Length;
            }
            else
            {
                referenceEndIndex = contents.IndexOf("/>", referenceStartIndex) + "/>".Length;
            }
            
            contents = contents.Remove(referenceStartIndex, referenceEndIndex - referenceStartIndex);

            string projectReference = "<ItemGroup>\n<ProjectReference Include=\""+projectPath+"\">\n"+
                "<Project>{" + projectGUID + "</Project>\n<Name>"+FileManager.RemoveExtension(FileManager.RemovePath(projectPath))+"</Name>\n</ProjectReference>\n</ItemGroup>\n"; 


            contents = contents.Insert(contents.IndexOf("<Import Project=\"$") - 1, projectReference);

            #endregion


        }

        private static string GetGUID(string projectLocation)
        {
            string projectContents = FileManager.FromFileText(projectLocation);
            int startIndex = projectContents.IndexOf("<ProjectGuid>{") + "<ProjectGuid>{".Length;
            int endIndex = projectContents.IndexOf("</ProjectGuid>");
            projectContents = projectContents.Substring(startIndex,endIndex-startIndex);              
            return projectContents;
 
        }

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

        /// <summary>
        /// Returns with default directory for templates (AppData) 
        /// </summary>
        /// <param name="zipToUnpack"></param>
        /// <param name="stringToReplace"></param>
        public static void GetDefaultZipLocationAndStringToReplace(PlatformProjectInfo project, out string zipToUnpack, out string stringToReplace)
        {
              GetDefaultZipLocationAndStringToReplace(project,FileManager.UserApplicationDataForThisApplication, out zipToUnpack, out stringToReplace);
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
                Directory.CreateDirectory(templateLocation);
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
    }
}
