using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Elements;

namespace FlatRedBall.Glue.FormHelpers.StringConverters
{
    public class AvailableClassTypeConverter : TypeConverter
    {

        //static string[] mFlatRedBallTypes = null;

        public static bool IsFlatRedBallType(string type)
        {
            return GetAvailableFrbClasses().Contains(type);
        }


        NamedObjectSave mContainer;

        public AvailableClassTypeConverter(NamedObjectSave container) : base()
        {
            mContainer = container;
        }

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }

        static List<string> stringToReturn = new List<string>();
        public override StandardValuesCollection
                     GetStandardValues(ITypeDescriptorContext context)
		{
            SourceType sourceType = SourceType.Entity;

            if (mContainer != null)
            {
                sourceType = mContainer.SourceType;
            }

            string[] availableClasses = GetAvailableTypes(true, sourceType);
            if (availableClasses != null)
            {
                stringToReturn.AddRange(availableClasses);
            }

            return new StandardValuesCollection(stringToReturn);
        }

        public static string[] GetAvailableTypes(bool includeNone, SourceType sourceType)
        {
            #region Instantiate variables

            stringToReturn.Clear();
            if (includeNone)
            {
                stringToReturn.Add("<NONE>");
            }
            
            string[] availableClasses = null;

            #endregion

            #region If FlatRedBallType

            if (sourceType == SourceType.FlatRedBallType)
            {
                availableClasses = GetAvailableFrbClasses();
            }

            #endregion

            #region else if Entity

            else if (sourceType == SourceType.Entity)
            {
                availableClasses = new string[ObjectFinder.Self.GlueProject.Entities.Count];

                for (int i = 0; i < ObjectFinder.Self.GlueProject.Entities.Count; i++)
                {
                    availableClasses[i] = ObjectFinder.Self.GlueProject.Entities[i].Name;
                }
            }

            #endregion


            return availableClasses.OrderBy(item => item).ToArray();
        }

        private static string[] GetAvailableFrbClasses()
        {
            List<string> toReturnAsList = new List<string>();

            var availableTypes = AvailableAssetTypes.Self.AllAssetTypes.ToList();

            foreach (var ati in availableTypes)
            {
                if (ati.CanBeObject)
                {
                    toReturnAsList.Add(ati.RuntimeTypeName);
                }
            }
            FlatRedBall.Utilities.StringFunctions.RemoveDuplicates(toReturnAsList);
            return toReturnAsList.ToArray();
        } 

    }
}
