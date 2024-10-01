using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.FormHelpers.StringConverters
{


    public class AvailableCustomVariables : TypeConverter
    {
        public bool IncludeNone
        {
            get;
            set;
        }

        public Predicate<CustomVariable> InclusionPredicate;
        

        IElement mElement;
		public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
		{
			return true;
		}

		public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
		{
			return true;
		}


        public AvailableCustomVariables(GlueElement element)
            : base()
        {
            IncludeNone = true;

            mElement = element;
        }


		List<string> stringToReturn = new List<string>();
		public override StandardValuesCollection
					 GetStandardValues(ITypeDescriptorContext context)
		{
			stringToReturn.Clear();
            if (IncludeNone)
            {
                stringToReturn.Add("<NONE>");
            }
            
            foreach (CustomVariable variable in mElement.CustomVariables)
            {
                if (InclusionPredicate == null || InclusionPredicate(variable))
                {
                    stringToReturn.Add(variable.Name);
                }
            }

			StandardValuesCollection svc = new StandardValuesCollection(stringToReturn);

			return svc;
		}


    }
}
