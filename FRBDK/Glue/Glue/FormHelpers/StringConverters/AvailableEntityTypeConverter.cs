using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Elements;

namespace FlatRedBall.Glue.FormHelpers.StringConverters
{
    /// <summary>
    /// Type converter returning all available entity names excluding CurrentEntitySave
    /// </summary>
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


        List<string> stringsToReturn = new List<string>();
        public override StandardValuesCollection
                     GetStandardValues(ITypeDescriptorContext context)
        {
            stringsToReturn.Clear();
            stringsToReturn.Add("<NONE>");


            for (int i = 0; i < ObjectFinder.Self.GlueProject.Entities.Count; i++)
            {
				string entityName = ObjectFinder.Self.GlueProject.Entities[i].Name;

                if (entityName != CurrentEntitySave.Name)
				{
					stringsToReturn.Add(entityName);
				}
            }

            return new StandardValuesCollection(stringsToReturn);
        } 
	}
}
