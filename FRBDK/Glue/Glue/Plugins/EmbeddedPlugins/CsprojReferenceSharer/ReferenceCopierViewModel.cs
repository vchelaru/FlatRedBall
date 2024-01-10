using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using FlatRedBall.Glue.MVVM;
using FlatRedBall.Glue.VSHelpers.Projects;
using Microsoft.Build.Evaluation;

namespace FlatRedBall.Glue.Plugins.EmbeddedPlugins.CsprojReferenceSharer
{
    public class ReferenceCopierViewModel : ViewModel
    {
        string fromFile;
        string toFile;

        public string FromFile
        {
            get
            {
                return fromFile;
            }
            set
            {
                this.ChangeAndNotify(ref fromFile, value, "FromFile");
            }
        }

        public string ToFile
        {
            get
            {
                return toFile;
            }
            set
            {
                this.ChangeAndNotify(ref toFile, value, "ToFile");
            }
        }

        public void PerformCopy(bool showPopup = true)
        {
            try
            {
                var from = Load(FromFile);
                var to = Load(ToFile);

                int referencesCopied = 0;

                foreach (var item in from.EvaluatedItems)
                {
                    bool shouldCopy = item.ItemType == "Compile" && 
                        FlatRedBall.IO.FileManager.GetExtension(item.UnevaluatedInclude) == "cs" &&
                        // Make sure we don't already have the file there:
                        to.CodeProject.IsFilePartOfProject(item.UnevaluatedInclude, BuildItemMembershipType.Any) == false;



                    if (shouldCopy)
                    {

                        var fullFileName = from.MakeAbsolute(item.UnevaluatedInclude);

                        // this is a class lib, no need to add to its codeproject
                        to.AddCodeBuildItem(fullFileName, true, fileRelativeToThis: item.UnevaluatedInclude);
                        PluginManager.ReceiveOutput($"Added {item.UnevaluatedInclude} to {ToFile}");
                        referencesCopied++;
                    }
                }


                if (referencesCopied != 0)
                {
                    to.Save(ToFile);
                }

                if(showPopup)
                {
                    MessageBox.Show("References successfully copied: " + referencesCopied);
                }
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message);
            }

        }

        ClassLibraryProject Load(string fileName)
        {
            var coreProject = new Project(fileName);

            ClassLibraryProject toReturn = new ClassLibraryProject(coreProject);

            toReturn.Load(fileName);

            return toReturn;
            
        }
    }
}
