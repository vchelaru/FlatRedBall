using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Utilities;
using FlatRedBall.IO;
using System.IO;

using System.ComponentModel;
using FlatRedBall.Glue.Events;

#if GLUE
using FlatRedBall.Glue.FormHelpers.PropertyGrids;
using FlatRedBall.Glue.FormHelpers;
using System.Windows.Forms;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.Elements;
#endif

namespace FlatRedBall.Glue.SaveClasses
{
    public enum ScreenOrEntity
    {
        Screen,
        Entity
    }

    public interface IElement : INamedObjectContainer, INameable, IFileReferencer, IEventContainer
    {
        //[Browsable(false)]
        //[BroadcastAttribute(BroadcastStaticOrInstance.Internal)]
        //string InitializeBroadcast
        //{
        //    get;
        //    set;
        //}
        
        //[Browsable(false)]
        //[BroadcastAttribute(BroadcastStaticOrInstance.Internal)]
        //string DestroyBroadcast
        //{
        //    get;
        //    set;
        //}

        List<CustomVariable> CustomVariables
        {
            get;
        }

        bool HasChanged { get; set; }

        IEnumerable<StateSave> AllStates
        {
            get;
        }

        List<PropertySave> Properties
        {
            get;
        }

        IEnumerable<NamedObjectSave> AllNamedObjects
        {
            get;
        }
        
        List<StateSave> States
        {
            get;
            set;
        }

        List<StateSaveCategory> StateCategoryList
        {
            get;
            set;
        }

        bool HasStates
        {
            get;
        }

        string ClassName
        {
            get;
            //set;
        }

        string BaseElement
        {
            get;
        }
       
    }

    #region Helper extension methods

    public static class IElementHelperMethods
    {
        public static int GetCount(this IEnumerable<StateSave> enumerable)
        {
            int returnCount = 0;
            foreach (StateSave stateSave in enumerable)
            {
                returnCount++;
            }
            return returnCount;
        }

		public static CustomVariable GetCustomVariable(this IElement element, string customVariableName)
		{
			foreach (CustomVariable customVariable in element.CustomVariables)
			{
				if (customVariable.Name == customVariableName)
				{
					return customVariable;
				}
			}
			return null;
		}

        
    }


    #endregion

}
