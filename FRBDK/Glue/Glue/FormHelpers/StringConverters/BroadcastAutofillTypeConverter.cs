using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using Glue;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Events;

namespace FlatRedBall.Glue.FormHelpers.StringConverters
{
    public class BroadcastAutofillTypeConverter : StringConverter
    {
        public override bool GetStandardValuesSupported(
               ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool GetStandardValuesExclusive(
                           ITypeDescriptorContext context)
        {
            return false;
        }

        List<string> stringsToReturn = new List<string>();
        public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            stringsToReturn.Clear();

            stringsToReturn.Add("<NONE>");

            IEventContainer eventContainer = null;// EditorLogic.CurrentNamedObject;

            if(eventContainer == null)
            {
                eventContainer = EditorLogic.CurrentScreenSave;
            }


            string appendText = "";


            string containerName = null;

            if (eventContainer != null)
            {
                containerName = FlatRedBall.IO.FileManager.RemovePath(eventContainer.Name);
            }
            else if (EditorLogic.CurrentEntitySave != null)
            {
                containerName = FlatRedBall.IO.FileManager.RemovePath(EditorLogic.CurrentEntitySave.Name);
            }


            if (eventContainer != null && eventContainer is NamedObjectSave)
            {
                if (((NamedObjectSave)eventContainer).GetCustomVariable(Form1.Self.PropertyGrid.SelectedGridItem.Label.Replace(" Set", "")) != null)
                {
                    appendText = "Set";
                }
            }
            string nameToPrepend = FlatRedBall.IO.FileManager.RemovePath(containerName);

            string nameFromLabel = Form1.Self.PropertyGrid.SelectedGridItem.Label.Replace(" Set", "");

            // Make a suggested name
            string suggestedName =
                nameToPrepend + nameFromLabel
                    + appendText;


            stringsToReturn.Add(suggestedName);



            return new StandardValuesCollection(stringsToReturn);
        }
        
    }
}
