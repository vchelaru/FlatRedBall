using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Elements;

namespace FlatRedBall.Glue.FormHelpers.StringConverters
{
    public class AvailableEntityTypeConverter : TypeConverter
	{

        public EntitySave CurrentEntitySave
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

        public AvailableEntityTypeConverter(EntitySave entitySave)
            : base()
        {
            CurrentEntitySave = entitySave;
        }


        List<string> stringToReturn = new List<string>();
        public override StandardValuesCollection
                     GetStandardValues(ITypeDescriptorContext context)
        {
            stringToReturn.Clear();
            stringToReturn.Add("<NONE>");


            for (int i = 0; i < ObjectFinder.Self.GlueProject.Entities.Count; i++)
            {
				string entityName = ObjectFinder.Self.GlueProject.Entities[i].Name;

                if (entityName != CurrentEntitySave.Name)
				{
					stringToReturn.Add(entityName);
				}
            }

            return new StandardValuesCollection(stringToReturn);
        } 
	}
}
