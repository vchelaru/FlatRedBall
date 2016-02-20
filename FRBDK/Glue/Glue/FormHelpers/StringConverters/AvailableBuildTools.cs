using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using FlatRedBall.Glue.GuiDisplay.Facades;

namespace FlatRedBall.Glue.FormHelpers.StringConverters
{
    public class AvailableBuildTools : TypeConverter
    {
        public bool ShowNoneOption { get; set; }

        public string SourceFileExtensionRestriction
        {
            get;
            set;
        }

        public bool ShowNewApplication
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

        public AvailableBuildTools()
        {
            ShowNewApplication = true;
        }

        public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            List<string> listToFill = new List<string>();
            listToFill.Clear();
            listToFill.AddRange(FacadeContainer.Self.ApplicationSettings.AvailableBuildTools);
            if (ShowNewApplication)
            {
                listToFill.Add("New Application...");
            }

            if (!string.IsNullOrEmpty(SourceFileExtensionRestriction))
            {
                // Only get build tools that start with the right extension
                string whatToStartWith = "*." + SourceFileExtensionRestriction;

                for (int i = listToFill.Count - 1; i > -1; i--)
                {
                    if (listToFill[i].StartsWith(whatToStartWith) == false)
                    {
                        listToFill.RemoveAt(i);
                    }
                }
            }

            if(ShowNoneOption)
            {
                listToFill.Insert(0, "<None>");
            }


			StandardValuesCollection svc = new StandardValuesCollection(listToFill);

			return svc;
        }
        
    }
}
