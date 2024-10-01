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

        public T GetProperty<T>(GlueElement element, string name)
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
                        GlueCommands.Self.FileCommands.OpenReferencedFileInDefaultProgram(GlueState.Self.CurrentReferencedFileSave);
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
                                try
                                {
                                    var startInfo = new ProcessStartInfo();
                                    startInfo.FileName = absolute.FullPath;
                                    startInfo.UseShellExecute = true;
                                    System.Diagnostics.Process.Start(startInfo);
                                }
                                // This can blow up on Linux if using wine:
                                catch(Exception e)
                                {
                                    GlueCommands.Self.PrintError(e.ToString());
                                }
                            }
                            handled = true;

                        }

                    }

                    #endregion
                }
            }
        }
    }
}
