using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.IO;
using FlatRedBall.Glue.Managers;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations.CommandInterfaces;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.VSHelpers.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FlatRedBall.Glue.CreatedClass
{
    public class CustomClassController : Singleton<CustomClassController>
    {
        public CustomClassSave GetCustomClassSaveIncludingThis(string fileName)
        {
            foreach (CustomClassSave customClassSave in ObjectFinder.Self.GlueProject.CustomClasses)
            {
                if (customClassSave.CsvFilesUsingThis.Contains(fileName))
                {
                    return customClassSave;
                }
            }

            return null;

        }



        public bool SetCsvRfsToUseCustomClass(ReferencedFileSave currentReferencedFile, CustomClassSave classToUse, bool force)
        {
            bool succeeded = false;

            if (classToUse != null)
            {
                succeeded = SetRfsToUseNonNullClass(currentReferencedFile, classToUse, force);
            }
            else
            {
                CustomClassSave oldCustomClass = null;
                DialogResult result = DialogResult.Yes;

                if (currentReferencedFile != null)
                {
                    oldCustomClass = GetCustomClassSaveIncludingThis(currentReferencedFile.Name);

                    result =
                        System.Windows.Forms.MessageBox.Show("Make the " + currentReferencedFile.Name + " file no longer use the " +
                        oldCustomClass.Name + " class?",
                        "Remove Custom Class Association",
                        MessageBoxButtons.YesNo);

                }

                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    if (currentReferencedFile == null)
                    {
                        MessageBox.Show("You've just encountered a Glue error that someone needs to fix.  Error details: " +
                            "Attempting to remove a ReferencedFileSave from a CustomClass, but the RFS doesn't actually exist");
                    }
                    else
                    {
                        oldCustomClass.CsvFilesUsingThis.Remove(currentReferencedFile.Name);
                        GluxCommands.Self.SaveGlux();
                        succeeded = true;
                    }
                }
            }

            return succeeded;
        }

        private bool SetRfsToUseNonNullClass(ReferencedFileSave currentReferencedFile, CustomClassSave classToUse, bool force)
        {
            bool succeeded = false;
            // See if this file is already using one Custom class, and if so, change it
            CustomClassSave customClassAlreadyBeingUsed = GetCustomClassSaveIncludingThis(
                currentReferencedFile.Name);
            if (customClassAlreadyBeingUsed != null)
            {
                // This could fail due to threading issues, so ignore failures:
                try
                {
                    customClassAlreadyBeingUsed.CsvFilesUsingThis.Remove(currentReferencedFile.Name);
                }
                catch(Exception e)
                {
                    int m = 3;
                }
                succeeded = true;
            }
            else
            {
                // This guy was using its own class, so let's tell the user and see if it should be removed
                string file = currentReferencedFile.GetTypeForCsvFile();

                // "file" is fully qualified, but we only want the non-qualified
                if(file.Contains("."))
                {
                    int lastDot = file.LastIndexOf('.');
                    file = file.Substring(lastDot + 1);
                }

                file = FlatRedBall.IO.FileManager.RelativeDirectory + "DataTypes/" + file + ".Generated.cs";

                if (ProjectManager.ProjectBase.IsFilePartOfProject(file, BuildItemMembershipType.CompileOrContentPipeline))
                {
                    DialogResult result;

                    if (force)
                    {
                        result = DialogResult.Yes;
                    }
                    else
                    {
                        result = MessageBox.Show("The CSV\n\n" + currentReferencedFile.Name + "\n\nwas using the file\n\n" +
                            file + "\n\nThis file is no associated with this CSV file.  Would you like to remove this file?", "Remove unused file?", MessageBoxButtons.YesNo);
                    }
                    if (result == System.Windows.Forms.DialogResult.Yes)
                    {
                        ProjectManager.ProjectBase.RemoveItem(file);
                        try
                        {
                            FileHelper.MoveToRecycleBin(file);
                        }
                        catch
                        {
                            PluginManager.ReceiveError("Could not delete file " + file);
                            // Even though the file couldn't be removed, we're going to succeed - the
                            // old file will remain there, and the CSV will use the new one.
                        }
                    }
                    succeeded = true;

                }
                else
                {
                    succeeded = true;
                }
            }

            classToUse.CsvFilesUsingThis.Add(currentReferencedFile.Name);
            return succeeded;
        }




    }


}
