using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using FlatRedBall.Glue;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.IO;

namespace OfficialPlugins.ProjectCopier
{
    public class CopyManager
    {
        bool saveOnNextCopy;
        ProjectCopierSettings mSettings;
        static DateTime lastCopy;
        private Action<string> showErrorAction;

        public event EventHandler AfterCopyFinished;

        public ProjectCopierSettings Settings
        {
            get
            { return mSettings; }
            set
            {
                mSettings = value;
            }
        }

        public string CurrentActivityInfo
        {
            get;
            private set;
        }

        public float PercentageFinished
        {
            get;
            private set;
        }

        public bool IsCopying
        {
            get;
            private set;
        }

        public bool ShouldContinue
        {
            get;
            set;
        }

        string CopySettingsLocation
        {
            get
            {
                // We don't want these to be shared through version control, so use the .user
                // extension to imply it's specific to each user and should be excluded from version control.
                return ProjectManager.ProjectSpecificSettingsFolder + "CopySettings.xml.user";
            }
        }
        
        public CopyManager(Action<string> showErrorAction)
        {
            if (showErrorAction == null) throw new ArgumentNullException(nameof(showErrorAction));

            this.showErrorAction = showErrorAction;
            mSettings = new ProjectCopierSettings();
        }

        private void CopyIndividualFile(string targetDirectory, ref int processedCount, ref int countCopied, ref int errorCount, ref int skippedCount, int totalCount, string fileUnmodified)
        {
            string whereToCopyFrom = mSettings.EffectiveSourceFolder;

            string relativeToSln = FileManager.MakeRelative(fileUnmodified, whereToCopyFrom);

            string destination = targetDirectory + relativeToSln;
            string destinationDirectory = FileManager.GetDirectory(destination);


            bool isOutOfDate = GetIfShouldCopyAccordingToDates(fileUnmodified, destination, Settings.CopyingDetermination);

            PercentageFinished = 100 * processedCount / (float)totalCount;

            string percentage = PercentageFinished.ToString("0.00") + "%";
            if (isOutOfDate)
            {
                CurrentActivityInfo = "Copying " + fileUnmodified.Replace("/", "\\");

                System.IO.Directory.CreateDirectory(destinationDirectory);

                bool succeeded = false;
                try
                {
                    System.IO.File.Copy(fileUnmodified, destination, true);
                    succeeded = true;
                }
                catch
                {
                    // If the file is encrypted via windows folder encryption, it most likely failed due to copying an encrypted file
                    // over the network (which requires user interaction).
                    var attributes = File.GetAttributes(fileUnmodified);
                    if (attributes.HasFlag(FileAttributes.Encrypted))
                    {
                        PluginManager.ReceiveOutput($"{percentage} Error copying to {destination} (original file is encrypted!)");
                    }
                    else
                    {
                        PluginManager.ReceiveOutput(percentage + " Error copying to " + destination);
                    }
                    
                    errorCount++;
                }
                if (succeeded)
                {
                    PluginManager.ReceiveOutput(percentage + " Copied " + destination);
                    countCopied++;
                }
            }
            else
            {
                CurrentActivityInfo = "Already up to date: " + fileUnmodified.Replace("/", "\\");

                // This is slow:
                //PluginManager.ReceiveOutput(percentage + " Up to date: " + destination);
                skippedCount++;
            }

            processedCount++;
        }

        private static bool GetIfShouldCopyAccordingToDates(string fileUnmodified, string destination, CopyingDetermination determination)
        {
            bool shouldCopy = true;

            if (determination == CopyingDetermination.SourceVsDestinationDates)
            {
                // Comparisons are good, so we'll keep doing it.
                //if (compareDates)
                //{
                var timespan = System.IO.File.GetLastWriteTime(fileUnmodified) - System.IO.File.GetLastWriteTime(destination);
                // It seems as if
                // the file system
                // on OSX doesn't keep
                // track of milliseconds
                // so a simple inequality
                // will fail.  We then just
                // ignore the milliseconds by 
                // giving the remote file an extra
                // second on the comparison.
                shouldCopy = timespan.TotalSeconds > 1;
                //}
            }
            else if(determination == CopyingDetermination.SinceLastCopy)
            {
                shouldCopy = System.IO.File.GetLastWriteTime(fileUnmodified) > lastCopy;
            }
            return shouldCopy;
        }

        private static List<string> GetListOfAllFilesToCopy(string whereToCopyFrom)
        {
            var allFiles = FileManager.GetAllFilesInDirectory(whereToCopyFrom);

            for (int i = allFiles.Count - 1; i > -1; i--)
            {
                if (ProjectCopierMainPlugin.ShouldExclude(allFiles[i]))
                {
                    allFiles.RemoveAt(i);
                }
            }
            return allFiles;
        }

        internal bool TryLoadOrCreateSettings()
        {
            bool succeeded = false;

            string fileToLoad = CopySettingsLocation;

            if (System.IO.File.Exists(fileToLoad))
            {
                try
                {
                    mSettings = FileManager.XmlDeserialize<ProjectCopierSettings>(fileToLoad);
                    succeeded = true;
                }
                catch(Exception e)
                {
                    PluginManager.ReceiveError(e.ToString());
                }
            }
            else
            {
                mSettings = new ProjectCopierSettings();
            }
            return succeeded;
        }

        internal void BeginCopying(bool shouldSave = true)
        {
            saveOnNextCopy = shouldSave;

            bool isCopying = false;

            //CopyItButton.Text = "Cancel Copy";

            System.Threading.Thread thread = new System.Threading.Thread(new ThreadStart(PerformCopy));
            thread.Start();

            isCopying = true;
        }

        public string GetWhyCantCopy()
        {
            string directory = mSettings.DestinationFolder;
            string whyIsntValid = null;
            if(string.IsNullOrEmpty(directory))
            {
                whyIsntValid = "Destination cannot be empty";
            }
            if(whyIsntValid == null && !System.IO.Directory.Exists(directory))
            {
                whyIsntValid = "Cannot find the directory\n\n" + directory + "\n\nDid you try accessing this directory through Windows Explorer?";
            }

            return whyIsntValid;
        }

        void PerformCopy()
        {
            if (saveOnNextCopy)
            {
                mSettings.Save(CopySettingsLocation);
            }

            string targetDirectory = mSettings.DestinationFolder;

            if (!targetDirectory.EndsWith("\\") && !targetDirectory.EndsWith("/"))
            {
                targetDirectory += "\\";

            }

            PercentageFinished = 0;
            IsCopying = true;

            ShouldContinue = true;
            string whereToCopyFrom = mSettings.EffectiveSourceFolder ;

            CurrentActivityInfo = "Obtaining list of files to copy...";

            var allFiles = GetListOfAllFilesToCopy(whereToCopyFrom);


            int processedCount = 0;
            int countCopied = 0;
            int errorCount = 0;
            int skippedCount = 0;
            int totalCount = allFiles.Count;

            foreach (string fileUnmodified in allFiles)
            {
                if (!ShouldContinue)
                {
                    break;
                }
                
                CopyIndividualFile(targetDirectory,
                    ref processedCount, ref countCopied, ref errorCount,
                    ref skippedCount, totalCount, fileUnmodified);

            }

            string errorText = "";
            if (errorCount != 0)
            {
                errorText = errorCount + " Errors.  ";
                var message = $"{errorCount} errors occurred while copying the project.  See output window for details";
                showErrorAction(message);
            }

            string output = errorText + countCopied + " Copied.  " + skippedCount + " Skipped.";
            if (!ShouldContinue)
            {
                output += "  User cancelled";
            }
            PluginManager.ReceiveOutput(output);
            IsCopying = false;
            if (AfterCopyFinished != null)
            {
                AfterCopyFinished(this, null);
            }

            lastCopy = System.DateTime.Now;
        }
    }
}
