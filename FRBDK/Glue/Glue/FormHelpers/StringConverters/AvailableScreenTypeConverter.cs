using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Elements;

namespace FlatRedBall.Glue.FormHelpers.StringConverters
{
    public class AvailableScreenTypeConverter : TypeConverter
    {
        ScreenSave mCurrentScreenSave;

        public ScreenSave CurrentScreenSave
        {
            get { return mCurrentScreenSave; }

            set
            {
                mCurrentScreenSave = value;
            }
        }

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }

        public AvailableScreenTypeConverter(ScreenSave currentScreenSave)
        {
            CurrentScreenSave = currentScreenSave;
        }

        List<string> stringToReturn = new List<string>();
        public override StandardValuesCollection
                     GetStandardValues(ITypeDescriptorContext context)
        {

            if (CurrentScreenSave == null)
            {
                throw new Exception("CurrentScreenSave must be set on the AvailableScreenTypeConverter before it is used.");
            }

            stringToReturn.Clear();
            stringToReturn.Add("<NONE>");

            ScreenSave currentScreenSave = CurrentScreenSave;

            for (int i = 0; i < ObjectFinder.Self.GlueProject.Screens.Count; i++)
            {
                string screenName = ObjectFinder.Self.GlueProject.Screens[i].Name;

                if (screenName != currentScreenSave.Name)
                {
                    stringToReturn.Add(screenName);
                }
            }

            return new StandardValuesCollection(stringToReturn);
        } 
    }
}
