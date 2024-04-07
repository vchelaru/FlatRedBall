using Renci.SshNet;
using Renci.SshNet.Sftp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace BuildServerUploaderConsole.Sftp
{
    static class SftpManager
    {
        public static IEnumerable<SftpFile> GetList(string host, string folder, string userName, string password)
        {

            using (var sftp = new SftpClient(host, userName, password))
            {
                sftp.ErrorOccurred += Sftp_ErrorOccurred;
                sftp.Connect();

                var toReturn = sftp.ListDirectory(folder).ToList();

                sftp.Disconnect();

                return toReturn;
            }
        }

        private static void Sftp_ErrorOccurred(object sender, Renci.SshNet.Common.ExceptionEventArgs e)
        {

        }

        public static SftpClient GetClient(string host, string userName, string password)
        {
            return new SftpClient(host, userName, password);

        }
        public static void UploadFile(string localFileToUpload, string host, string targetFile, string userName, string password, Action<ulong> uploadCallback)
        {
            using (var sftp = new SftpClient(host, userName, password))
            {
                sftp.OperationTimeout = new TimeSpan(0, 0, seconds: 40);
                sftp.Connect();
                UploadFileWithOpenConnection(localFileToUpload, targetFile, sftp, uploadCallback);

                sftp.Disconnect();
            }
        }

        public static void UploadFileWithOpenConnection(string localFileToUpload, string targetFile, SftpClient sftp, Action<ulong> uploadCallback)
        {
            var directory = FlatRedBall.IO.FileManager.GetDirectory(targetFile, FlatRedBall.IO.RelativeType.Relative);

            CreateDirectoriesRecursively(directory, sftp);

            using (var file = File.OpenRead(localFileToUpload))
            {
                sftp.UploadFile(file, targetFile, canOverride: true, uploadCallback);
            }
        }

        private static void CreateDirectoriesRecursively(string directory, SftpClient sftp)
        {
            if (sftp.Exists(directory) == false)
            {
                // try creating one above, in case we need it:
                var directoryAbove = FlatRedBall.IO.FileManager.GetDirectory(directory, FlatRedBall.IO.RelativeType.Relative);

                CreateDirectoriesRecursively(directoryAbove, sftp);

                sftp.CreateDirectory(directory);
            }
        }

        public static void DeleteRemoteDirectory(string host, string directory, string username, string password)
        {
            using (var sftp = new SftpClient(host, username, password))
            {
                sftp.Connect();

                sftp.DeleteDirectory(directory);

                sftp.Disconnect();

            }
        }

        public static void DeleteRemoteFile(string host, string file, string username, string password)
        {
            using (var sftp = new SftpClient(host, username, password))
            {
                sftp.Connect();

                sftp.Delete(file);

                sftp.Disconnect();

            }
        }


    }
}
