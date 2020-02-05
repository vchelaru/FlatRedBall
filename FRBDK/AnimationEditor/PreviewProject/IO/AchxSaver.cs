using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FlatRedBall.AnimationEditorForms;
using FlatRedBall.Content.AnimationChain;
using FlatRedBall.Glue.Controls;
using FlatRedBall.IO;
using FlatRedBall.Utilities;

namespace PreviewProject.IO
{
    public class AchxSaver : Singleton<AchxSaver>
    {
        public void InitateSaveProcess(string oldFileName, MainControl mainControl)
        {
            string fileName = AskUserWhereToSave();

            if (!string.IsNullOrEmpty(fileName))
            {
                // If the directory has changed, we need to ask the user
                // what to do with file references
                string newDirectory = FileManager.Standardize(FileManager.GetDirectory(fileName));
                string projectFileName = oldFileName;
                bool isNewDirectory = string.IsNullOrEmpty(projectFileName);
                string projectDirectory = null;

                if (!string.IsNullOrEmpty(projectFileName))
                {
                    projectDirectory = FileManager.GetDirectory(projectFileName);

                    projectDirectory = FileManager.Standardize(projectDirectory);

                    isNewDirectory = projectDirectory != newDirectory;
                }

                bool shouldSave = false;

                if (isNewDirectory && !string.IsNullOrEmpty(projectDirectory))
                {
                    AnimationChainListSave achs = mainControl.AnimationChainList; 
                    List<string> files = GetAllFilesIn(achs, projectDirectory);

                    if (files.Count != 0)
                    {
                        shouldSave = PerformCopyFileRelativeLogic(newDirectory, projectDirectory, shouldSave, achs, files);
                    }
                    else
                    {
                        shouldSave = true;
                    }
                }
                else
                {
                    shouldSave = true;
                }
                if (shouldSave)
                {
                    mainControl.SaveCurrentAnimationChain(fileName);

                    WireframeManager.Self.RefreshAll();
                }
            }

        }

        private static bool PerformCopyFileRelativeLogic(string newDirectory, string projectDirectory, bool shouldSave, AnimationChainListSave achs, List<string> files)
        {
            MultiButtonMessageBox mbmb = new MultiButtonMessageBox();
            mbmb.MessageText = "What would you like to do with files referenced by the .achx?";

            mbmb.AddButton("Copy them relative", DialogResult.Yes);
            mbmb.AddButton("Leave them where they are (make relative references)", DialogResult.No);

            DialogResult result = mbmb.ShowDialog();


            AnimationChainListSave cloned = FileManager.CloneObject(achs);



            foreach (var animationChain in achs.AnimationChains)
            {
                foreach (var animationFrame in animationChain.Frames)
                {
                    if (FileManager.IsRelative(animationFrame.TextureName))
                    {
                        animationFrame.TextureName = projectDirectory + animationFrame.TextureName;
                    }
                }
            }

            StringFunctions.RemoveDuplicates(files);

            if (result == DialogResult.Yes)
            {
                // Copy these all over, then make all files relative
                foreach (string file in files)
                {
                    if (System.IO.File.Exists(file))
                    {
                        string destination = newDirectory + FileManager.RemovePath(file);
                        System.IO.File.Copy(file, destination, true);
                    }
                }
                foreach (var animationChain in achs.AnimationChains)
                {
                    foreach (var animationFrame in animationChain.Frames)
                    {
                        animationFrame.TextureName = FileManager.RemovePath(animationFrame.TextureName);
                    }
                }
                shouldSave = true;
            }
            else if (result == DialogResult.No)
            {
                foreach (var animationChain in achs.AnimationChains)
                {
                    foreach (var animationFrame in animationChain.Frames)
                    {
                        animationFrame.TextureName = FileManager.MakeRelative(animationFrame.TextureName, newDirectory);
                    }
                }
                shouldSave = true;
            }
            else if (result == DialogResult.Cancel)
            {
                for (int animationIndex = 0; animationIndex < cloned.AnimationChains.Count; animationIndex++)
                {
                    for (int frameIndex = 0; frameIndex < cloned.AnimationChains[animationIndex].Frames.Count; animationIndex++)
                    {
                        ProjectManager.Self.AnimationChainListSave.AnimationChains[animationIndex].Frames[frameIndex].TextureName =
                            cloned.AnimationChains[animationIndex].Frames[frameIndex].TextureName;
                    }
                }
            }
            return shouldSave;
        }

        private static string AskUserWhereToSave()
        {

            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = "Animation Chain (*.achx)|*.achx";
            string fileName = null;

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                fileName = dialog.FileName;
            }
            return fileName;
        }

        private static List<string> GetAllFilesIn(AnimationChainListSave achs, string directoryToMakeRelativeTo)
        {
            List<string> files = new List<string>();
            foreach (var animationChain in achs.AnimationChains)
            {
                foreach (var animationFrame in animationChain.Frames)
                {
                    if (FileManager.IsRelative(animationFrame.TextureName))
                    {
                        files.Add(directoryToMakeRelativeTo +  animationFrame.TextureName);
                    }
                }
            }
            return files;
        }


    }
}
