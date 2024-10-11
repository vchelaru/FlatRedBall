using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Utilities;
using FlatRedBall.IO;
using System.IO;

using System.ComponentModel;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.Interfaces;

namespace FlatRedBall.Glue.SaveClasses
{
    #region Enums

    public enum ScreenOrEntity
    {
        Screen,
        Entity
    }

    #endregion

    /// <summary>
    /// An interface which defines methods and properties common to Glue Screens and Entities.
    /// </summary>
    /// <remarks>
    /// This was introduced in the early days of Glue, but now work is moving to the GlueElement abstract class. It's best to 
    /// move out of interface into that abstract class to avoid having to place default implementations in this interface.
    /// </remarks>
    public interface IElement : INamedObjectContainer, INameable, IFileReferencer, IEventContainer, IPropertyListContainer
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

        bool IsHiddenInTreeView { get; set; }


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

        public static string GetStrippedName(this IElement element)
        {
            var lastSlash = element.Name.LastIndexOf('\\');

            // +1 to exclude the slash
            return element.Name.Substring(lastSlash + 1);
        }

        public static string GetNameWithoutTypePrefix(this IElement element)
        {
            var fullName = element.Name;

            if(element is ScreenSave)
            {
                return fullName.Substring("Screens\\".Length);
            }
            else if(element is EntitySave)
            {
                return fullName.Substring("Entities\\".Length);
            }
            throw new Exception();
        }
        
    }


    #endregion

}
