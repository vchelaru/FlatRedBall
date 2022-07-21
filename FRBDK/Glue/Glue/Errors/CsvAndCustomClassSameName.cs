using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.Glue.SaveClasses;
using GluePropertyGridClasses.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.Errors
{
    public class CsvAndCustomClassSameName : ErrorViewModel
    {
        ReferencedFileSave File { get; set; }
        CustomClassSave CustomClass { get; set; }

        public override string UniqueId => $"{File.Name} {CustomClass?.Name}";

        public CsvAndCustomClassSameName(ReferencedFileSave rfs, 
            CustomClassSave customClass)
        {
            File = rfs;
            CustomClass = customClass;

            Details = $"The file {rfs} has the same name as the custom class " +
                $"{customClass}, but the file does not use the custom class. " +
                $"This can result in codegen errors.";
        }

        public override bool GetIfIsFixed()
        {
            var isError =
                IsError(File, CustomClass);

            return isError == false;
        }

        public static bool IsError(ReferencedFileSave file, CustomClassSave customClass)
        {
            var glueProject = GlueState.Self.CurrentGlueProject;


            if (glueProject == null)
            {
                return false;
            }
            string className;

            if (!string.IsNullOrEmpty(customClass.CustomNamespace))
            {
                className = customClass.CustomNamespace + "." + customClass.Name;
            }
            else
            {
                className = EditorObjects.IoC.Container.Get<IVsProjectState>().DefaultNamespace +
                ".DataTypes." + customClass.Name;
            }
            var areSameName = file.GetTypeForCsvFile() == className;

            if (areSameName == false)
            {
                return false;
            }
            if (ObjectFinder.Self.GetAllReferencedFiles().Contains(file) == false)
            {
                return false;
            }
            if (glueProject.CustomClasses.Contains(customClass) == false)
            {
                return false;
            }
            if (customClass.CsvFilesUsingThis.Contains(file.Name))
            {
                return false;
            }

            return true;
        }

        public override void HandleDoubleClick()
        {
            GlueState.Self.CurrentReferencedFileSave = File;
        }
    }
}
