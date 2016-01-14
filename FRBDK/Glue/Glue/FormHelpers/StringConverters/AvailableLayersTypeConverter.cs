using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.FormHelpers.StringConverters
{
    public class AvailableLayersTypeConverter : TypeConverter
    {
        public IElement CurrentElement
        {
            get;
            set;
        }


        public const string UnderEverythingLayerName = "Under Everything (Engine Layer)";
        public const string UnderEverythingLayerCode = "SpriteManager.UnderAllDrawnLayer";

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            
            return true;
        }

        List<string> stringToReturn = new List<string>();

        public AvailableLayersTypeConverter(IElement currentElement)
            : base()
        {
            CurrentElement = currentElement;
        }


        public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            stringToReturn.Clear();
            stringToReturn.Add("<NONE>");
            stringToReturn.Add(UnderEverythingLayerName);

            if (CurrentElement != null)
            {
                foreach (NamedObjectSave namedObjectSave in CurrentElement.NamedObjects)
                {
                    if (namedObjectSave.IsLayer)
                    {
                        stringToReturn.Add(namedObjectSave.InstanceName);
                    }
                }


                return new StandardValuesCollection(stringToReturn);
            }

            return base.GetStandardValues(context);
        }
    }
}
