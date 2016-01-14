using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using FlatRedBall.IO.Remote;
using FlatRedBall.IO;

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

        public const string p = "48iJ" + "vB!t";
        private readonly DateTime _deleteBeforeDate;
        private string _ftpFolder =
                "flatredball.com/content/FrbXnaTemplates/";
        private readonly string _backupFolder =
                "flatredball.com/content/FrbXnaTemplates/";
        private readonly string _ftpCopyToFolder = null;
        private readonly string _backupFile =
            "ftp://flatredball.com/flatredball.com/content/FrbXnaTemplates/BackupFolders.txt";

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

            CleanUpBackups();
        }

        private bool FolderExists(string fileName)
        {
            var files = FtpManager.GetList("ftp://flatredball.com/" + _backupFolder, "frbadmin", p);

            return files.Any(fileStruct => "ftp://flatredball.com/" + _backupFolder + fileStruct.Name == fileName);
        }

        private void CleanUpBackups()
        {
            //Get files in folder
            var files = FtpManager.GetList("ftp://flatredball.com/" + _backupFolder, "frbadmin", p);

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
                        DeleteDirectory("ftp://flatredball.com/" + _backupFolder + fileStruct.Name);
                    }
                }
            }
        }

        private void DeleteDirectory(string path)
        {
            //Get files
            var files = FtpManager.GetList(path, "frbadmin", p);

            //Loop through files
            foreach (var fileStruct in files)
            {
                //If directory, need to delete sub directories
                if (fileStruct.IsDirectory)
                {
                    DeleteDirectory(path + "/" + fileStruct.Name);
                }
                else
                {
                    FtpManager.DeleteRemoteFile(path + "/" + fileStruct.Name, "frbadmin", p);
                }
            }

            DeleteFtpDirectory(path, "frbadmin", p);
        }

        /// <summary>
        /// method to delete folder on the FTP server
        /// Note: the directory should be empty.
        /// </summary>
        /// <param name="strFtpPath">FTP server path i.e: ftp://yourserver/foldername</param>
        /// <param name="strftpUserId">username</param>
        /// <param name="strftpPassword">password</param>
        public void DeleteFtpDirectory(string strFtpPath, string strftpUserId, string strftpPassword)
        {
            // dirName = name of the directory to create.

            var reqFtp = (FtpWebRequest)WebRequest.Create(new Uri(strFtpPath));
            reqFtp.Method = WebRequestMethods.Ftp.RemoveDirectory;
            reqFtp.UseBinary = true;
            reqFtp.Credentials = new NetworkCredential(strftpUserId, strftpPassword);
            var response = (FtpWebResponse)reqFtp.GetResponse();
            Stream ftpStream = response.GetResponseStream();

            if (ftpStream != null) ftpStream.Close();
            response.Close();
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
            BuildBackupFile();
        }

        private void BuildBackupFile()
        {
            string path = Path.GetTempPath() + @"\BackupFolders.txt";

            StreamWriter sw;
            sw = File.CreateText(path);

            //Filename structure
            var exp = new Regex(@"^build_\d\d\d\d_\d\d_\d\d_\d\d$");

            //Daily Builds
            var files = FtpManager.GetList("ftp://flatredball.com/flatredball.com/content/FrbXnaTemplates/DailyBackups/", "frbadmin", p);
            var folderNames = (from fileStruct in files where fileStruct.IsDirectory && exp.IsMatch(fileStruct.Name) select fileStruct.Name).ToList();
            folderNames.Reverse();
            foreach (var folderName in folderNames)
            {
                //Get year
                var year = int.Parse(folderName.Substring(6, 4));

                //Get month
                var month = int.Parse(folderName.Substring(11, 2));

                //Get day
                var day = int.Parse(folderName.Substring(14, 2));

                //Get version
                var version = int.Parse(folderName.Substring(17, 2));

                sw.WriteLine("Daily Build - " + month + @"/" + day + @"/" + year + " " + version + ",DailyBackups/" + folderName + "/");
            }

            //Weekly Builds
            files = FtpManager.GetList("ftp://flatredball.com/flatredball.com/content/FrbXnaTemplates/WeeklyBackups/", "frbadmin", p);
            folderNames = (from fileStruct in files where fileStruct.IsDirectory && exp.IsMatch(fileStruct.Name) select fileStruct.Name).ToList();
            folderNames.Reverse();
            foreach (var folderName in folderNames)
            {
                //Get year
                var year = int.Parse(folderName.Substring(6, 4));

                //Get month
                var month = int.Parse(folderName.Substring(11, 2));

                //Get day
                var day = int.Parse(folderName.Substring(14, 2));

                //Get version
                var version = int.Parse(folderName.Substring(17, 2));

                sw.WriteLine("Weekly Build - " + month + @"/" + day + @"/" + year + " " + version + ",WeeklyBackups/" + folderName + "/");
            }

            //Monthly Builds
            files = FtpManager.GetList("ftp://flatredball.com/flatredball.com/content/FrbXnaTemplates/MonthlyBackups/", "frbadmin", p);
            folderNames = (from fileStruct in files where fileStruct.IsDirectory && exp.IsMatch(fileStruct.Name) select fileStruct.Name).ToList();
            folderNames.Reverse();
            foreach (var folderName in folderNames)
            {
                //Get year
                var year = int.Parse(folderName.Substring(6, 4));

                //Get month
                var month = int.Parse(folderName.Substring(11, 2));

                //Get day
                var day = int.Parse(folderName.Substring(14, 2));

                //Get version
                var version = int.Parse(folderName.Substring(17, 2));

                sw.WriteLine("Monthly Build - " + month + @"/" + day + @"/" + year + " " + version + ",MonthlyBackups/" + folderName + "/");
            }

            sw.Close();

            FtpManager.UploadFile(path, _backupFile, "frbadmin", p);
        }

        private void UploadFrbdkFiles()
        {
            string localFile = ZipFrbdk.DestinationFile;

            string fileName = FileManager.RemovePath(localFile);

            string destination = _ftpFolder + fileName;
            FtpManager.UploadFile(
                localFile, "ftp://flatredball.com", "frbadmin", p, destination, false);

            Results.WriteMessage(localFile + " uploaded to " + destination);


        }

        private void UploadEngineFiles()
        {
            List<CopyInformation> engineFiles = CopyBuiltEnginesToReleaseFolder.CopyInformationList;

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

                FtpManager.UploadFile(
                    localFile, "ftp://flatredball.com", "frbadmin", p, destination, false);


                Results.WriteMessage(engineFiles[i].DestinationFile + " uploaded to " + destination);
            }
        }

        private void UploadTemplateFiles()
        {
            string templateDirectory = DirectoryHelper.ReleaseDirectory + @"ZippedTemplates/";

            foreach (var file in Directory.GetFiles(templateDirectory, "*.zip"))
            {
                var fileName = FileManager.RemovePath(file);

                string localFile = templateDirectory + fileName;
                string destination = _ftpFolder + "ZippedTemplates/" + fileName;
                FtpManager.UploadFile(
                    localFile, "ftp://flatredball.com", "frbadmin", p, destination, false);


                Results.WriteMessage(file + " uploaded to " + destination);
            }
        }
    }
}
