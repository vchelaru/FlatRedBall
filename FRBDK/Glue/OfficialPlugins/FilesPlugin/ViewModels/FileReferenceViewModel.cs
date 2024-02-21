using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using Glue;
using GlueFormsCore.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.FilesPlugin.ViewModels
{
    public class FileReferenceViewModel : ViewModel
    {
        public ObservableCollection<string> ReferencedFiles
        {
            get;
            set;
        } = new ObservableCollection<string>();

        ReferencedFileSave referencedFileSave;
        public ReferencedFileSave ReferencedFileSave
        {
            get
            {
                return referencedFileSave;
            }
            set
            {
                if (value != referencedFileSave)
                {
                    referencedFileSave = value;
                    base.NotifyPropertyChanged(nameof(ReferencedFileSave));

                    ReferencedFiles.Clear();
                    ReferencedFiles.Add("Click Refresh to see all referenced files...");
                }
            }
        }

        public void Refresh()
        {
            ReferencedFiles.Clear();
            if (ReferencedFileSave != null)
            {
               TaskManager.Self.Add(() =>
               {
                   var contentDirectory = GlueState.Self.ContentDirectory;

                   var files = GlueCommands.Self.FileCommands.GetFilesReferencedBy(ReferencedFileSave, EditorObjects.Parsing.TopLevelOrRecursive.Recursive);

                   files = files
                       .Select(item =>
                       {
                           if (FileManager.IsRelativeTo(item.FullPath, contentDirectory))
                           {
                               return FileManager.MakeRelative(item.FullPath, contentDirectory);
                           }
                           else
                           {
                               return item;
                           }
                       })
                        .Distinct()

                       .OrderBy(item => item.FullPath);

                   MainPanelControl.Self.Invoke(() =>
                   {
                       foreach (var file in files)
                       {
                          ReferencedFiles.Add(file.FullPath);
                       }
                   });
               },
               "Refreshing unreferenced files");
            }
        }
    }
}
