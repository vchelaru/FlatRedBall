using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using FlatRedBall.IO.Remote;
using FlatRedBall.IO;
using BuildServerUploaderConsole.Sftp;

namespace BuildServerUploaderConsole.Processes
{
    public enum UploadType
    {
        DailyBuild,
        Monthly,
        Weekly
    }

    public class UploadFilesToFrbServer : ProcessStep
    {
        string absolutePath = @"C:\FlatRedBallProjects\UploadInfo.txt";

        string cachedPassword = null;
        public string Password
        {
            get
            {
                if (cachedPassword == null)
                {
                    var doesFileExist = System.IO.File.Exists(absolutePath);

                    if (doesFileExist)
                    {
                        ReadUsernameAndPassword();
                    }
                    else
                    {
                        throw new Exception($"Could not find the file {absolutePath}. This file is needed and should contain the password to the FRB server.");
                    }
                }

                return cachedPassword;
            }
        }

        string cachedUsername = null;
        public string Username
        {
            get
            {
                if(cachedUsername == null)
                {
                    var doesFileExist = System.IO.File.Exists(absolutePath);

                    if (doesFileExist)
                    {
                        ReadUsernameAndPassword();
                    }
                    else
                    {
                        throw new Exception($"Could not find the file {absolutePath}. This file is needed and should contain the password to the FRB server.");
                    }
                }

                return cachedUsername;
            }
        }

        private void ReadUsernameAndPassword()
        {
            string fileContents = System.IO.File.ReadAllText(absolutePath);
            var split = fileContents.Split(' ');

            cachedUsername = split[0];
            cachedPassword = split[1];
        }

        private readonly DateTime _deleteBeforeDate;
        private string _ftpFolder =
                "files.flatredball.com/content/FrbXnaTemplates/";
        private readonly string _backupFolder =
                "files.flatredball.com/content/FrbXnaTemplates/";
        private readonly string _ftpCopyToFolder = null;
        //private const string host = "sftp://flatredball.com/";
        private const string host = "flatredball.com";
        private const string _backupFile =
            "flatredball.com/content/FrbXnaTemplates/BackupFolders.txt";

        public UploadFilesToFrbServer(IResults results, UploadType uploadType)
            : base(
            @"Upload files to daily build location", results)
        {
            int number = 1;
            string fileName = "build_" + DateTime.Now.ToString("yyyy") + "_" + DateTime.Now.ToString("MM") + "_" +
                DateTime.Now.ToString("dd") + "_";

            switch (uploadType)
            {
                case UploadType.Monthly:
                    _deleteBeforeDate = DateTime.MinValue;
                    _ftpFolder += "MonthlyBackups/";
                    _backupFolder += "MonthlyBackups/";
                    break;
                case UploadType.Weekly:
                    _deleteBeforeDate = DateTime.Now.AddMonths(-1);
                    _ftpFolder += "WeeklyBackups/";
                    _backupFolder += "WeeklyBackups/";
                    break;
                default:
                    _deleteBeforeDate = DateTime.Now.AddDays(-7);
                    _ftpFolder += "DailyBackups/";
                    _backupFolder += "DailyBackups/";
                    _ftpCopyToFolder = "flatredball.com/content/FrbXnaTemplates/DailyBuild/";
                    break;
            }

            while (FolderExists(_ftpFolder + fileName + number.ToString("00")))
            {
                number++;
            }
            _ftpFolder += fileName + number.ToString("00") + "/";

            // who cares about cleaning up backups? We have infinite storage, this takes time, and it's crashing as oif
            // December 12, 2015
            //CleanUpBackups();
        }

        private bool FolderExists(string fileName)
        {
            var files = SftpManager.GetList(host, _backupFolder, Username, Password);

            return files.Any(fileStruct => "sftp://files.flatredball.com/" + _backupFolder + fileStruct.Name == fileName);
        }

        private void CleanUpBackups()
        {
            //Get files in folder
            var files = SftpManager.GetList(host, _backupFolder, Username, Password);

            //Filename structure
            var exp = new Regex(@"^build_\d\d\d\d_\d\d_\d\d_\d\d$");

            //Loop through directory
            foreach (var fileStruct in files)
            {
                //If a directory and matches filename structure
                if (fileStruct.IsDirectory && exp.IsMatch(fileStruct.Name))
                {
                    //Get year
                    var year = int.Parse(fileStruct.Name.Substring(6, 4));

                    //Get month
                    var month = int.Parse(fileStruct.Name.Substring(11, 2));

                    //Get day
                    var day = int.Parse(fileStruct.Name.Substring(14, 2));

                    //Get version
                    var version = int.Parse(fileStruct.Name.Substring(17, 2));

                    //Get date from file name
                    var date = new DateTime(year, month, day);

                    //If past expiration date
                    if (date < _deleteBeforeDate)
                    {
                        //Remove file from server
                        DeleteDirectory(_backupFolder + fileStruct.Name);
                    }
                }
            }
        }

        private void DeleteDirectory(string relativeDirectory)
        {
            //Get files
            var files = SftpManager.GetList(host, relativeDirectory, Username, Password);

            //Loop through files
            foreach (var fileStruct in files)
            {
                //If directory, need to delete sub directories
                if (fileStruct.IsDirectory)
                {
                    DeleteDirectory(relativeDirectory + "/" + fileStruct.Name);
                }
                else
                {
                    SftpManager.DeleteRemoteFile(host, fileStruct.Name, Username, Password);
                }
            }

            SftpManager.DeleteRemoteDirectory(host, relativeDirectory, Username, Password);
        }

        public override void ExecuteStep()
        {
            UploadFrbdkFiles();
            UploadEngineFiles();
            UploadTemplateFiles();

            //Check to see if files need to be copied to another folder
            if (_ftpCopyToFolder != null)
            {
                _ftpFolder = _ftpCopyToFolder;
                UploadFrbdkFiles();
                UploadEngineFiles();
                UploadTemplateFiles();
            }
            // this times out, and not sure we really need it anyway...
            //BuildBackupFile();
        }

        //private void BuildBackupFile()
        //{
        //    string localPath = Path.GetTempPath() + @"\BackupFolders.txt";

        //    StreamWriter sw;
        //    sw = File.CreateText(localPath);

        //    //Filename structure
        //    var exp = new Regex(@"^build_\d\d\d\d_\d\d_\d\d_\d\d$");

        //    //Daily Builds
        //    var files =
        //        SftpManager.GetList(host, "flatredball.com/content/FrbXnaTemplates/DailyBackups/", Username, Password);
        //    var folderNames = (from fileStruct in files where fileStruct.IsDirectory && exp.IsMatch(fileStruct.Name) select fileStruct.Name).ToList();
        //    folderNames.Reverse();
        //    foreach (var folderName in folderNames)
        //    {
        //        //Get year
        //        var year = int.Parse(folderName.Substring(6, 4));

        //        //Get month
        //        var month = int.Parse(folderName.Substring(11, 2));

        //        //Get day
        //        var day = int.Parse(folderName.Substring(14, 2));

        //        //Get version
        //        var version = int.Parse(folderName.Substring(17, 2));

        //        sw.WriteLine("Daily Build - " + month + @"/" + day + @"/" + year + " " + version + ",DailyBackups/" + folderName + "/");
        //    }

        //    //Weekly Builds
        //    files = SftpManager.GetList(host, "flatredball.com/content/FrbXnaTemplates/WeeklyBackups/", Username, Password);
        //    folderNames = (from fileStruct in files where fileStruct.IsDirectory && exp.IsMatch(fileStruct.Name) select fileStruct.Name).ToList();
        //    folderNames.Reverse();
        //    foreach (var folderName in folderNames)
        //    {
        //        //Get year
        //        var year = int.Parse(folderName.Substring(6, 4));

        //        //Get month
        //        var month = int.Parse(folderName.Substring(11, 2));

        //        //Get day
        //        var day = int.Parse(folderName.Substring(14, 2));

        //        //Get version
        //        var version = int.Parse(folderName.Substring(17, 2));

        //        sw.WriteLine("Weekly Build - " + month + @"/" + day + @"/" + year + " " + version + ",WeeklyBackups/" + folderName + "/");
        //    }

        //    //Monthly Builds
        //    files = SftpManager.GetList(
        //        host, "flatredball.com/content/FrbXnaTemplates/MonthlyBackups/", Username, Password);
        //    folderNames = (from fileStruct in files where fileStruct.IsDirectory && exp.IsMatch(fileStruct.Name) select fileStruct.Name).ToList();
        //    folderNames.Reverse();
        //    foreach (var folderName in folderNames)
        //    {
        //        //Get year
        //        var year = int.Parse(folderName.Substring(6, 4));

        //        //Get month
        //        var month = int.Parse(folderName.Substring(11, 2));

        //        //Get day
        //        var day = int.Parse(folderName.Substring(14, 2));

        //        //Get version
        //        var version = int.Parse(folderName.Substring(17, 2));

        //        sw.WriteLine("Monthly Build - " + month + @"/" + day + @"/" + year + " " + version + ",MonthlyBackups/" + folderName + "/");
        //    }

        //    sw.Close();

        //    SftpManager.UploadFile(localPath, host, _backupFile, Username, Password);
        //}

        private void UploadFrbdkFiles()
        {
            string localFile = ZipFrbdk.DestinationFile;

            string fileName = _ftpFolder + FileManager.RemovePath(localFile);

            SftpManager.UploadFile(
                localFile, host, fileName, Username, Password);

            Results.WriteMessage(localFile + " uploaded to " + fileName);


        }

        private void UploadEngineFiles()
        {
            List<CopyInformation> engineFiles = CopyBuiltEnginesToReleaseFolder.CopyInformationList;

            using (var client = SftpManager.GetClient(host, Username, Password))
            {
                client.Connect();
                for (int i = 0; i < engineFiles.Count; i++)
                {
                    string localFile = engineFiles[i].DestinationFile;

                    string engineName = FileManager.RemovePath(FileManager.GetDirectory(localFile));
                    string debugOrRelease = null;
                    if (engineName.ToLower() == "debug/" || engineName.ToLower() == "release/")
                    {
                        debugOrRelease = engineName;
                        engineName = FileManager.RemovePath(FileManager.GetDirectory(FileManager.GetDirectory(localFile)));
                    }


                    string fileName = engineName + debugOrRelease +
                        FileManager.RemovePath(localFile);
                    string destination = _ftpFolder + "SingleDlls/" + fileName;

                    SftpManager.UploadFileWithOpenConnection(
                        localFile, destination, client);


                    Results.WriteMessage(engineFiles[i].DestinationFile + " uploaded to " + destination);
                }
                client.Disconnect();
            }
        }

        private void UploadTemplateFiles()
        {
            string templateDirectory = DirectoryHelper.ReleaseDirectory + @"ZippedTemplates/";

            using (var client = SftpManager.GetClient(host, Username, Password))
            {
                client.Connect();


                foreach (var file in Directory.GetFiles(templateDirectory, "*.zip"))
                {
                    var fileName = FileManager.RemovePath(file);

                    string localFile = templateDirectory + fileName;
                    string destination = _ftpFolder + "ZippedTemplates/" + fileName;
                    SftpManager.UploadFileWithOpenConnection(
                        localFile, destination, client);


                    Results.WriteMessage(file + " uploaded to " + destination);
                }

                client.Disconnect();
            }
        }
    }
}
