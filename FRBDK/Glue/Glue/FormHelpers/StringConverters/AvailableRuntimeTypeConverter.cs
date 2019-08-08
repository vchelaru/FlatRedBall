using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Plugins;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.FormHelpers.StringConverters
{
    class AvailableRuntimeTypeConverter : TypeConverter
    {
        string Extension => FlatRedBall.IO.FileManager.GetExtension(ReferencedFileSave.Name);

        public ReferencedFileSave ReferencedFileSave
        {
            get; set;
        }

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }

        public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            List<string> toReturn = new List<string>();

            foreach (var ati in AvailableAssetTypes.Self.AllAssetTypes)
            {
                if (ati.Extension == this.Extension)
                {
                    toReturn.Add(ati.QualifiedRuntimeTypeName.QualifiedType);
                }
            }

            var additionalAssetTypeNames = PluginManager
                .GetAvailableAssetTypes(ReferencedFileSave)
                .Select(item => item.QualifiedRuntimeTypeName.QualifiedType);

            toReturn.AddRange(additionalAssetTypeNames);

            return new StandardValuesCollection(toReturn);
        }
    }
}
