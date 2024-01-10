using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.IO;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.Elements;
using EditorObjects.Parsing;
using FlatRedBall.Glue.GuiDisplay.Facades;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.Plugins.ExportedImplementations;

namespace FlatRedBall.Glue.FormHelpers
{
    public class AvailableNameablesStringConverter : TypeConverter
    {
        public NamedObjectSave NamedObjectSave
        {
            get;
            set;
        }

        public ReferencedFileSave ReferencedFileSave
        {
            get;
            set;

        }

        public override bool GetStandardValuesSupported(
                       ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool GetStandardValuesExclusive(
                           ITypeDescriptorContext context)
        {
            return true;
        }

        public AvailableNameablesStringConverter(NamedObjectSave namedObjectSave, ReferencedFileSave referencedFileSave)
            : base()
        {
            NamedObjectSave = namedObjectSave;
            this.ReferencedFileSave = referencedFileSave;
        }


        public override System.ComponentModel.TypeConverter.StandardValuesCollection
                     GetStandardValues(ITypeDescriptorContext context)
        {
            List<string> stringToReturn = GetAvailableNamedObjectSourceNames(NamedObjectSave);
            return new System.ComponentModel.TypeConverter.StandardValuesCollection(stringToReturn);
        }

        public static List<string> GetAvailableNamedObjectSourceNames(NamedObjectSave namedObject)
        {
            List<string> listOfObjectsToReturn = new List<string>();
            listOfObjectsToReturn.Add("<NONE>");

            if (namedObject != null && !string.IsNullOrEmpty(namedObject.SourceFile))
            {
                string relativeFile = namedObject.SourceFile;

                FillListWithAvailableObjects(relativeFile, listOfObjectsToReturn);
            }

            return listOfObjectsToReturn;
        }

        public static void FillListWithAvailableObjects(string relativeFile, List<string> listOfObjectsToReturn)
        {

            string referencedFile = GlueState.Self.ContentDirectory + relativeFile;

            var fileExtension = FileManager.GetExtension(relativeFile);

            var matchingAtis = AvailableAssetTypes.Self.AllAssetTypes
                .Where(ati => ati.Extension == fileExtension )
                .ToList();

            //if(referencedFile?.GetAssetTypeInfo().CanBeObject == true)
            //{

            //}

            foreach (AssetTypeInfo ati in matchingAtis)
            {
                listOfObjectsToReturn.Add("Entire File (" + ati.RuntimeTypeName + ")");

            }


            // We'll have "entire file" be first, then alphabetize the rest:
            List<string> tempList = new List<string>();
            PluginManager.TryAddContainedObjects(referencedFile, tempList);
            tempList.Sort();
            listOfObjectsToReturn.AddRange(tempList);
        } 



    }
}
