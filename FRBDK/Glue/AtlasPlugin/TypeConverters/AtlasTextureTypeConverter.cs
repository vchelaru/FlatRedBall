using FlatRedBall.Glue.Plugins.ExportedImplementations;
using FlatRedBall.IO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AtlasPlugin.TypeConverters
{
    class AtlasTextureTypeConverter : TypeConverter
    {

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            List<string> availableValues = new List<string>();

            foreach (var rfs in GlueState.Self.CurrentGlueProject.GetAllReferencedFiles()
                .Where(item=>FileManager.GetExtension(item.Name) == "atlas"))
            {
                var absoluteFile = GlueCommands.Self.GetAbsoluteFileName(rfs);

                if(File.Exists(absoluteFile))
                {
                    AddFilesFromAtlasToList(absoluteFile, availableValues);
                }
            }

            StandardValuesCollection svc = new StandardValuesCollection(availableValues);

            return svc;
        }

        private void AddFilesFromAtlasToList(string absoluteFile, List<string> availableValues)
        {
            foreach(var line in System.IO.File.ReadLines(absoluteFile)
                .Where(item=>item.StartsWith("#") == false && string.IsNullOrEmpty(item) == false)
                .Select(item=>item.Split(';')))
            {
                availableValues.Add(line[0]);
            }
        }
    }
}
