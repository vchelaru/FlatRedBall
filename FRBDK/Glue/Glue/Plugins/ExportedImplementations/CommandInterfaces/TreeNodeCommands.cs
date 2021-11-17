﻿using System;
using System.Diagnostics;
using System.Windows.Forms;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using Glue;
using HQ.Util.Unmanaged;

namespace FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces
{
    class TreeNodeCommands : ITreeNodeCommands
    {
        public void SelectTreeNode(TreeNode treeNode)
        {
            MainGlueWindow.Self.BeginInvoke(new EventHandler(delegate { ElementViewWindow.SelectedNodeOld = treeNode; }));
        }

        public void SetProperty<T>(string name, T value)
        {
            if(GlueState.Self.CurrentEntitySave != null)
            {
                var currentEntity = GlueState.Self.CurrentEntitySave;

                SetProperty(currentEntity, name, value);
            }else if(GlueState.Self.CurrentScreenSave != null)
            {
                var currentScreen = GlueState.Self.CurrentScreenSave;

                SetProperty(currentScreen, name, value);
            }else
            {
                throw new Exception("Unable to set property for this tree node type.");   
            }
        }

        public void SetProperty<T>(EntitySave entitySave, string name, T value)
        {
            string newValue;

            FileManager.XmlSerialize(value, out newValue);

            entitySave.Properties.SetValue(name, newValue);
        }

        public void SetProperty<T>(ScreenSave screenSave, string name, T value)
        {
            string newValue;

            FileManager.XmlSerialize(value, out newValue);

            screenSave.Properties.SetValue(name, newValue);
        }

        public T GetProperty<T>(string name)
        {
            if (GlueState.Self.CurrentEntitySave != null)
            {
                var currentEntity = GlueState.Self.CurrentEntitySave;

                return GetProperty<T>(currentEntity, name);
            }
            
            if (GlueState.Self.CurrentScreenSave != null)
            {
                var currentScreen = GlueState.Self.CurrentScreenSave;

                return GetProperty<T>(currentScreen, name);
            }

            throw new Exception("Unable to get property for this tree node type.");
        }

        public T GetProperty<T>(EntitySave entitySave, string name)
        {
            var returnValue = (string)entitySave.Properties.GetValue(name);

            return !String.IsNullOrEmpty(returnValue) ? FileManager.XmlDeserializeFromString<T>(returnValue) : default(T);
        }

        public T GetProperty<T>(ScreenSave screenSave, string name)
        {
            var returnValue = (string)screenSave.Properties.GetValue(name);

            return !String.IsNullOrEmpty(returnValue) ? FileManager.XmlDeserializeFromString<T>(returnValue) : default(T);
        }

        public T GetProperty<T>(IElement element, string name)
        {
            if(element is ScreenSave)
            {
                return GetProperty<T>(element as ScreenSave, name);
            }
            
            if(element is EntitySave)
            {
                return GetProperty<T>(element as EntitySave, name);
            }

            return default(T);
        }

        public void HandleTreeNodeDoubleClicked(ITreeNode treeNode)
        {
            if (treeNode != null)
            {
                string text = treeNode.Text;

                var handled = PluginManager.TryHandleTreeNodeDoubleClicked(treeNode);

                if(!handled)
                {
                    #region Double-clicked a file
                    string extension = FileManager.GetExtension(text);
                
                    if (GlueState.Self.CurrentReferencedFileSave != null && !string.IsNullOrEmpty(extension))
                    {
                        HandleFileTreeNodeDoubleClick(text);
                        handled = true;
                    }

                    #endregion
                }

                if(!handled)
                {

                    #region Code

                    if (treeNode.IsCodeNode())
                    {
                        var fileName = treeNode.Text;

                        var absolute = GlueState.Self.CurrentGlueProjectDirectory + fileName;

                        if (System.IO.File.Exists(absolute))
                        {
                            var startInfo = new ProcessStartInfo();
                            startInfo.FileName = absolute;
                            startInfo.UseShellExecute = true;
                            System.Diagnostics.Process.Start(startInfo);
                        }
                        handled = true;
                    }

                    #endregion
                }
            }
        }

        private static void HandleFileTreeNodeDoubleClick(string text)
        {
            string textExtension = FileManager.GetExtension(text);
            string sourceExtension = null;

            if (GlueState.Self.CurrentReferencedFileSave != null && !string.IsNullOrEmpty(GlueState.Self.CurrentReferencedFileSave.SourceFile))
            {
                sourceExtension = FileManager.GetExtension(GlueState.Self.CurrentReferencedFileSave.SourceFile);
            }

            var effectiveExtension = sourceExtension ?? textExtension;


            string applicationSetInGlue = "";

            ReferencedFileSave currentReferencedFileSave = GlueState.Self.CurrentReferencedFileSave;
            string fileName;

            if (currentReferencedFileSave != null && currentReferencedFileSave.OpensWith != "<DEFAULT>")
            {
                applicationSetInGlue = currentReferencedFileSave.OpensWith;
            }
            else
            {
                applicationSetInGlue = EditorData.FileAssociationSettings.GetApplicationForExtension(effectiveExtension);
            }

            if (currentReferencedFileSave != null)
            {
                if (!string.IsNullOrEmpty(currentReferencedFileSave.SourceFile))
                {
                    fileName =
                        ProjectManager.MakeAbsolute(ProjectManager.ContentDirectoryRelative + currentReferencedFileSave.SourceFile, true);
                }
                else
                {
                    fileName = ProjectManager.MakeAbsolute(ProjectManager.ContentDirectoryRelative + currentReferencedFileSave.Name);
                }
            }
            else
            {
                fileName = ProjectManager.MakeAbsolute(text);
            }

            if (string.IsNullOrEmpty(applicationSetInGlue) || applicationSetInGlue == "<DEFAULT>")
            {
                try
                {
                    var executable = WindowsFileAssociation.GetExecFileAssociatedToExtension(effectiveExtension);

                    if (string.IsNullOrEmpty(executable))
                    {
                        var message = $"Windows does not have an association for the extension {effectiveExtension}. You must set the " +
                            $"program to associate with this extension to open the file. Set the assocaition now?";

                        GlueCommands.Self.DialogCommands.ShowYesNoMessageBox(message, OpenProcess);
                    }
                    else
                    {
                        OpenProcess();
                    }

                    void OpenProcess()
                    {
                        var startInfo = new ProcessStartInfo();
                        startInfo.FileName = "\"" + fileName + "\"";
                        startInfo.UseShellExecute = true;

                        System.Diagnostics.Process.Start(startInfo);
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show("Error opening " + fileName + "\nTry navigating to this file and opening it through explorer");


                }
            }
            else
            {
                bool applicationFound = true;
                try
                {
                    applicationSetInGlue = FileManager.Standardize(applicationSetInGlue);
                }
                catch
                {
                    applicationFound = false;
                }

                if (!System.IO.File.Exists(applicationSetInGlue) || applicationFound == false)
                {
                    string error = "Could not find the application\n\n" + applicationSetInGlue;

                    System.Windows.Forms.MessageBox.Show(error);
                }
                else
                {
                    MessageBox.Show("This functionality has been removed as of March 7, 2021. If you need it, please talk to Vic on Discord.");
                    //ProcessManager.OpenProcess(applicationSetInGlue, fileName);
                }
            }
        }


    }
}
