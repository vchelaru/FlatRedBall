using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.SaveClasses;


namespace FlatRedBall.Glue.FormHelpers.StringConverters
{
    public class AvailableClassGenericTypeConverter : TypeConverter
	{

        public List<EntitySave> EntitiesToExclude
        {
            get;
            set;
        }


        public AvailableClassGenericTypeConverter()
        {
            EntitiesToExclude = new List<EntitySave>();
        }


		public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
		{
			return true;
		}

		public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
		{
			return true;
		}

		public override System.ComponentModel.TypeConverter.StandardValuesCollection
					 GetStandardValues(ITypeDescriptorContext context)
		{
            List<string> valuesToReturn = GetAvailableValues(true);
			
            
			return new System.ComponentModel.TypeConverter.StandardValuesCollection(valuesToReturn);
		}

        public List<string> GetAvailableValues(bool includeNone)
        {
            List<string> valuesToReturn = new List<string>();

            valuesToReturn.Clear();

            if (includeNone)
            {
                valuesToReturn.Add("<NONE>");
            }


            foreach (var entity in ObjectFinder.Self.GlueProject.Entities.Where(item=>EntitiesToExclude.Contains(item) == false))
            {
                valuesToReturn.Add(entity.Name);
            }

            foreach(var assetType in AvailableAssetTypes.Self.AllAssetTypes)
            {
                if(assetType.IsPositionedObject)
                {
                    valuesToReturn.Add(assetType.QualifiedRuntimeTypeName.QualifiedType);
                }
            }

            return valuesToReturn;
        } 
	}
}
