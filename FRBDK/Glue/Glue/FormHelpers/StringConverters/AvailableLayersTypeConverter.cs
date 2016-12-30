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
        public const string UnderEverythingLayerCode = "FlatRedBall.SpriteManager.UnderAllDrawnLayer";

        public const string TopLayerName = "Top Layer (Engine Layer)";
        public const string TopLayerCode = "FlatRedBall.SpriteManager.TopLayer";

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            
            return true;
        }

        List<string> stringsToReturn = new List<string>();

        public AvailableLayersTypeConverter(IElement currentElement)
            : base()
        {
            CurrentElement = currentElement;
        }


        public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            stringsToReturn.Clear();
            stringsToReturn.Add("<NONE>");
            stringsToReturn.Add(UnderEverythingLayerName);

            if (CurrentElement != null)
            {
                foreach (NamedObjectSave namedObjectSave in CurrentElement.NamedObjects)
                {
                    if (namedObjectSave.IsLayer)
                    {
                        stringsToReturn.Add(namedObjectSave.InstanceName);
                    }
                }

                stringsToReturn.Add(TopLayerName);

                return new StandardValuesCollection(stringsToReturn);
            }

            return base.GetStandardValues(context);
        }
    }
}
