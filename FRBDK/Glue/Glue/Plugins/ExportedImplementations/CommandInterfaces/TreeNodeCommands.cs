using System;
using System.Diagnostics;
using System.Linq;
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
                        HandleFileTreeNodeDoubleClick(GlueState.Self.CurrentReferencedFileSave);
                        handled = true;
                    }

                    #endregion
                }

                if(!handled)
                {

                    #region Code

                    if (treeNode.IsCodeNode())
                    {
                        var element = treeNode.GetContainingElementTreeNode()?.Tag as GlueElement;
                        if(element != null)
                        {
                            var elementDirectory = GlueCommands.Self.FileCommands.GetCustomCodeFilePath(element).GetDirectoryContainingThis();
                            FilePath absolute = elementDirectory + treeNode.Text;

                            if (absolute.Exists())
                            {
                                var startInfo = new ProcessStartInfo();
                                startInfo.FileName = absolute.FullPath;
                                startInfo.UseShellExecute = true;
                                System.Diagnostics.Process.Start(startInfo);
                            }
                            handled = true;

                        }

                    }

                    #endregion
                }
            }
        }

        private static void HandleFileTreeNodeDoubleClick(ReferencedFileSave currentReferencedFileSave)
        {
            string textExtension = FileManager.GetExtension(currentReferencedFileSave.Name);
            string sourceExtension = null;

            if (GlueState.Self.CurrentReferencedFileSave != null && !string.IsNullOrEmpty(GlueState.Self.CurrentReferencedFileSave.SourceFile))
            {
                sourceExtension = FileManager.GetExtension(GlueState.Self.CurrentReferencedFileSave.SourceFile);
            }

            var effectiveExtension = sourceExtension ?? textExtension;
            string fileName = GetFileName(currentReferencedFileSave);

            string applicationSetInGlue = "";
            if (currentReferencedFileSave != null && currentReferencedFileSave.OpensWith != "<DEFAULT>")
            {
                applicationSetInGlue = currentReferencedFileSave.OpensWith;
            }
            else
            {
                applicationSetInGlue = EditorData.FileAssociationSettings.GetApplicationForExtension(effectiveExtension);
            }
            if (string.IsNullOrEmpty(applicationSetInGlue) || applicationSetInGlue == "<DEFAULT>")
            {
                try
                {
                    var executable = WindowsFileAssociation.GetExecFileAssociatedToExtension(effectiveExtension);

                    if (string.IsNullOrEmpty(executable) && !WindowsFileAssociation.NativelyHandledExtensions.Contains(effectiveExtension))
                    {
                        //Attempt to get relative gum project
                        var ideGum = GlueState.Self.GlueExeDirectory + "../../../../../../Gum/Gum/bin/Debug/Data/Gum.exe";
                        if(System.IO.File.Exists(ideGum)) {
                            Process.Start(new ProcessStartInfo(ideGum, fileName));
                            return;
                        }

                        var message = $"Windows does not have an association for the extension {effectiveExtension}. You must set the " +
                            $"program to associate with this extension to open the file. Set the assocaition now?";

                        var result = GlueCommands.Self.DialogCommands.ShowYesNoMessageBox(message);
                        if(result == System.Windows.MessageBoxResult.Yes)
                        {
                            OpenProcess();
                        }
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

        private static string GetFileName(ReferencedFileSave currentReferencedFileSave)
        {
            string fileName = null;
            if (currentReferencedFileSave != null)
            {
                if (!string.IsNullOrEmpty(currentReferencedFileSave.SourceFile))
                {
                    fileName =
                        GlueCommands.Self.GetAbsoluteFileName(ProjectManager.ContentDirectoryRelative + currentReferencedFileSave.SourceFile, true);
                }
                else
                {
                    fileName = GlueCommands.Self.GetAbsoluteFileName(currentReferencedFileSave);
                }
            }

            return fileName;
        }
    }
}
