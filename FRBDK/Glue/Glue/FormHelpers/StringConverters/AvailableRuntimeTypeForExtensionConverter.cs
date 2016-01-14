using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using FlatRedBall.Glue.Elements;

namespace FlatRedBall.Glue.FormHelpers.StringConverters
{
    class AvailableRuntimeTypeForExtensionConverter  : TypeConverter
    {
        public string Extension
        {
            get;
            set;
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

            return new StandardValuesCollection(toReturn);
        }
    }
}
