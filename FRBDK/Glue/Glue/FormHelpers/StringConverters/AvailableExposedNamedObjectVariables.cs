using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Reflection;

namespace FlatRedBall.Glue.FormHelpers.StringConverters
{
    public class AvailableExposedNamedObjectVariables : TypeConverter
    {
        CustomVariable mCustomVariable;
        IElement  mElement;



		public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
		{
			return true;
		}

		public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
		{
			return true;
		}

        public AvailableExposedNamedObjectVariables(CustomVariable customVariable, GlueElement element)
            : base()
        {
            mCustomVariable = customVariable;
            mElement = element;
        }

		List<string> stringToReturn = new List<string>();
		public override StandardValuesCollection
					 GetStandardValues(ITypeDescriptorContext context)
		{
			stringToReturn.Clear();
            stringToReturn.Add("<NONE>");


            string nameOfNamedObject = mCustomVariable.SourceObject;
            NamedObjectSave nos = mElement.GetNamedObjectRecursively(nameOfNamedObject);

            if (nos != null)
            {
                stringToReturn.AddRange(ExposedVariableManager.GetExposableMembersFor(nos).Select(item=>item.Member));
            }

			StandardValuesCollection svc = new StandardValuesCollection(stringToReturn);

			return svc;
		} 


    }
}
