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

namespace FlatRedBall.Glue.FormHelpers
{
    class AvailableNameablesStringConverter : TypeConverter
    {
        public NamedObjectSave NamedObjectSave
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

        public AvailableNameablesStringConverter(NamedObjectSave namedObjectSave)
            : base()
        {
            NamedObjectSave = namedObjectSave;
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

            string referencedFile = FacadeContainer.Self.ProjectValues.ContentDirectory + relativeFile;

            var fileExtension = FileManager.GetExtension(relativeFile);

            foreach (AssetTypeInfo ati in AvailableAssetTypes.Self.AllAssetTypes.Where(ati => ati.Extension == fileExtension))
            {
                listOfObjectsToReturn.Add("Entire File (" + ati.RuntimeTypeName + ")");

            }


#if GLUE
            
            PluginManager.TryAddContainedObjects(referencedFile, listOfObjectsToReturn);
#else
                //ContentParser.GetNamedObjectsIn(referencedFile, stringToReturn);
#endif
        } 



    }
}
