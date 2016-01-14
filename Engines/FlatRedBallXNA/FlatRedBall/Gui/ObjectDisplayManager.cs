using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Reflection;
using FlatRedBall.Utilities;

#if !SILVERLIGHT

using FlatRedBall.Gui.PropertyGrids;
#endif

namespace FlatRedBall.Gui
{

    public delegate string StringRepresentation(object objectToGetStringFor);

	#region NewWindowLimitation enum

	#region XML Docs
	/// <summary>
    /// Enum used by the ObjectDisplayManager to indicate how
    /// old IObjectDisplayers are treated when new IObjectDisplayers are created.
    /// </summary>
    #endregion
    public enum NewWindowLimitation
    {
        ByType,
        ByRequestingWindow,
        NoLimitation
	}

	#endregion

	// Vic asks:  Why isn't this class a static class?  
    public class ObjectDisplayManager
    {
        #region Fields

        Dictionary<object, IObjectDisplayer> mObjectDisplayers = new Dictionary<object,IObjectDisplayer>();

        Dictionary<IObjectDisplayer, IObjectDisplayer> mWindowFamilies = new Dictionary<IObjectDisplayer, IObjectDisplayer>();

        #region XML Docs
        /// <summary>
        /// The methods to call for each type to get the string representation for referenced objects.
        /// </summary>
        #endregion
        static internal Dictionary<Type, StringRepresentation> sStringRepresentations =
            new Dictionary<Type, StringRepresentation>();


        private NewWindowLimitation mNewWindowLimitation = NewWindowLimitation.ByType;

        #endregion

        #region Properties

        public static string ContentManagerName
        {
            get;
            set;
        }

        public NewWindowLimitation NewWindowLimitation
        {
            get { return mNewWindowLimitation; }
            set { mNewWindowLimitation = value; }
        }

        #endregion

        #region Event

        public static event GuiMessage AnyNewWindowCreated;

        #endregion

        #region Event Methods

        private void RemovalOfWatchedWindow(Window callingWindow)
        {
            IObjectDisplayer asObjectDisplayer = callingWindow as IObjectDisplayer;

            GuiManager.RemoveWindow(callingWindow);

            if (asObjectDisplayer.ObjectDisplayingAsObject != null &&
                mObjectDisplayers.ContainsKey(asObjectDisplayer.ObjectDisplayingAsObject))
            {
                mObjectDisplayers.Remove(asObjectDisplayer.ObjectDisplayingAsObject);
            }
            else
            {
                // We still need to see if this Window is contained in this dictionary even
                // if the previous if statement fails.  The reason for this is because of value
                // and immutable types.  For example, a String PropertyGrid might open up for the
                // value "".  Afterwards, the user changes the value and then double-clicks a list
                // to open a new PropertyGrid.  Since the string has changed, then asObjectDisplayer.ObjectDisplayingAsObject
                // will no longer equal the "" string so the Window will never be removed when it should be.
                object keyToRemove = null;

                foreach (KeyValuePair<object, IObjectDisplayer> kvp in mObjectDisplayers)
                {
                    if (kvp.Value == asObjectDisplayer)
                    {
                        keyToRemove = kvp.Key;
                        break;
                    }
                }

                if (keyToRemove != null)
                {
                    mObjectDisplayers.Remove(keyToRemove);
                }

            }
            


            if (mWindowFamilies.ContainsValue(asObjectDisplayer))
            {
                // Remove the object by its value
                IObjectDisplayer keyToRemove = null;

                foreach (KeyValuePair<IObjectDisplayer, IObjectDisplayer> kvp in mWindowFamilies)
                {
                    if (kvp.Value == asObjectDisplayer)
                    {
                        keyToRemove = kvp.Key;
                        break;
                    }
                }

                if (keyToRemove != null)
                {
                    mWindowFamilies.Remove(keyToRemove);

                }
            }
        }

        #endregion

        #region Methods

        #region Constructor
        // Internal - The GuiManager is the only thing that should have an instance of this
        internal ObjectDisplayManager()
        {


        }

        #endregion

        #region Public Methods

        public static ListDisplayWindow CreateListDisplayWindowForObject(object displayedObject, Cursor cursor)
        {
            ListDisplayWindow listDisplayWindow = new ListDisplayWindow(cursor);
            listDisplayWindow.HasMoveBar = true;
            listDisplayWindow.HasCloseButton = true;
            listDisplayWindow.Resizable = true;
            listDisplayWindow.ShowPropertyGridOnStrongSelect = true;
            listDisplayWindow.PrependIndex = true;

            listDisplayWindow.ListShowing = displayedObject as IEnumerable;

            // try to enable adding and removing - this is so common we're going to make it the default
            listDisplayWindow.EnableAddingToList();
            listDisplayWindow.EnableRemovingFromList();
            return listDisplayWindow;
        }

        public IObjectDisplayer GetObjectDisplayerForObject(object objectToDisplay)
        {
            return GetObjectDisplayerForObject(objectToDisplay, null, null);
        }

        public IObjectDisplayer GetObjectDisplayerForObject(object objectToDisplay, Window requestingWindow)
        {
            return GetObjectDisplayerForObject(objectToDisplay, requestingWindow, null);
        }

        public IObjectDisplayer GetObjectDisplayerForObject(object objectToDisplay, Window requestingWindow, string memberName)
        {
            bool throwAway;
            return GetObjectDisplayerForObject(objectToDisplay, requestingWindow, memberName, out throwAway);

        }

        public IObjectDisplayer GetObjectDisplayerForObject(object objectToDisplay, Window requestingWindow, string memberName, out bool isNewWindow)
        {
            IObjectDisplayer requestingWindowAsObjectDisplayer = requestingWindow as IObjectDisplayer;
            if (mObjectDisplayers.ContainsKey(objectToDisplay))
            {
                isNewWindow = false;
                return mObjectDisplayers[objectToDisplay];
            }
            else //Definitely creating a new ObjectDisplayer.
            {
                #region Remove existing windows depending on the new window limitation
                switch (mNewWindowLimitation)
                {
                    #region By Type
                    case NewWindowLimitation.ByType:
                        // if there is already something showing this type, get rid of it
                        Type typeOfObject = objectToDisplay.GetType();

                        foreach (KeyValuePair<object, IObjectDisplayer> kvp in mObjectDisplayers)
                        {
                            if (kvp.Key.GetType() == typeOfObject)
                            {
                                if (mObjectDisplayers[kvp.Key] is Window)
                                {
                                    ((Window)mObjectDisplayers[kvp.Key]).CloseWindow();
                                }
                                else
                                {
                                    throw new NotImplementedException("We don't currently support IObjectDisplayers that are not Windows.");
                                }
                                
                                break;
                            }
                        }
                        break;
                    #endregion

                    #region By Requesting Window
                    case NewWindowLimitation.ByRequestingWindow:
                        if (requestingWindow != null)
                        {

                            if (mWindowFamilies.ContainsKey(requestingWindowAsObjectDisplayer))
                            {
                                Window windowToGetRidOf =
                                    mWindowFamilies[requestingWindowAsObjectDisplayer] as Window;

                                // Any Window that is created through this object
                                // will have its Closing event remove itself from the
                                // GuiManager.
                                windowToGetRidOf.CloseWindow();
                            }
                        }
                        break;
                    #endregion

                    default:
                        break;
                }
                #endregion

                #region Create the new IObjectDisplayer, add it to the by-object dictionary, and assign it its ObjectDisplaying
                isNewWindow = true;
                IObjectDisplayer newObjectDisplayer = CreateObjectDisplayer(objectToDisplay, requestingWindow, memberName);

                newObjectDisplayer.ObjectDisplayingAsObject = objectToDisplay;
                mObjectDisplayers.Add(objectToDisplay, newObjectDisplayer);
                #endregion

                #region If the newly-created object displayer is a Window, then add it to the GuiManager and give it the appropriate events
                if (newObjectDisplayer is Window)
                {
                    Window asWindow = newObjectDisplayer as Window;

                    GuiManager.AddWindow(asWindow);
                    asWindow.HasCloseButton = true;
                    asWindow.HasMoveBar = true;

                    asWindow.Closing += RemovalOfWatchedWindow;
                }
                #endregion

                #region We have a parent window of this new object
                if (mNewWindowLimitation == NewWindowLimitation.ByRequestingWindow && requestingWindow != null)
                {
                    mWindowFamilies.Add(requestingWindowAsObjectDisplayer, newObjectDisplayer);
                }
                #endregion

                return newObjectDisplayer;
            }
        }

        public static string GetStringRepresentationFor(object objectToGetRepresentationFor)
        {
            Type type = objectToGetRepresentationFor.GetType();
            if(sStringRepresentations.ContainsKey(type))
            {
                return sStringRepresentations[type](objectToGetRepresentationFor);
            }
            else if(objectToGetRepresentationFor is INameable)
            {
                return ((INameable)objectToGetRepresentationFor).Name;
            }
            else
            {
                return objectToGetRepresentationFor.ToString();
            }

        }

        public void HideObjectDisplayerForObject(object objectToHideDisplayerFor)
        {
            if (mObjectDisplayers.ContainsKey(objectToHideDisplayerFor))
            {
                ((Window)mObjectDisplayers[objectToHideDisplayerFor]).CloseWindow();
            }
        }

        public void RegisterObjectDisplayer(IObjectDisplayer objectDisplayer)
        {
            if (objectDisplayer.ObjectDisplayingAsObject == null)
            {
                throw new InvalidOperationException("The IObjectDisplayer has a null ObjectDisplaying");
            }
            else
            {
                mObjectDisplayers.Add(objectDisplayer.ObjectDisplayingAsObject, objectDisplayer);
            }
        }

        public void SetNewObjectDisplaying(IObjectDisplayer objectDisplayer, object newObjectDisplaying)
        {
            if (newObjectDisplaying == null)
            {
                throw new InvalidOperationException("Registered IObjectDisplayers cannot have a null ObjectDisplaying");
            }            
            
            // It's ok if objectDisplayer had a null ObjectDisplayingAsObject because that just means
            // it wasn't part of the mObjectDisplayers before.  But now it's being added.
            if (objectDisplayer.ObjectDisplayingAsObject != null && 
                mObjectDisplayers.ContainsKey(objectDisplayer.ObjectDisplayingAsObject))
            {
                mObjectDisplayers.Remove(objectDisplayer);
            }

            objectDisplayer.ObjectDisplayingAsObject = newObjectDisplaying;

            mObjectDisplayers.Add(newObjectDisplaying, objectDisplayer);

        }

		public static void SetStringRepresentationMethod(Type type, StringRepresentation stringRepresentation)
		{
			if (sStringRepresentations.ContainsKey(type))
			{
				sStringRepresentations[type] = stringRepresentation;
			}
			else
			{
				sStringRepresentations.Add(type, stringRepresentation);
			}
		}

        #endregion

        #region Internal Methods

        internal void Activity()
        {
            foreach (IObjectDisplayer displayer in mObjectDisplayers.Values)
            {
                displayer.UpdateToObject();
            }
        }


        internal IObjectDisplayer CreateObjectDisplayer(object displayedObject, Window requestingWindow, string memberName)
        {
            IObjectDisplayer objectToReturn = null;


            PropertyGrid newGrid = null;
            ListDisplayWindow newListDisplayWindow = null;

            Type objectType = displayedObject.GetType();
            Type propertyGridType;

            ListDisplayWindow requestingListDisplayWindow = requestingWindow as ListDisplayWindow;
            PropertyGrid requestingPropertyGrid = requestingWindow as PropertyGrid;

            #region Get the cursor to use for the new Window

            Cursor cursor = GuiManager.Cursor;

            if(requestingWindow != null)
            {
                cursor = requestingWindow.mCursor;
            }

            #endregion


            #region Create the IDisplayWindow depending on the type of object passed

            #region If object is enum

            if (objectType.IsEnum)
            {
#if SILVERLIGHT
                throw new NotImplementedException();
#else
                propertyGridType = typeof(EnumPropertyGrid<>).MakeGenericType(objectType);

#if XBOX360
                throw new NotImplementedException("No PropertyGrid support in ObjectDisplayManager on 360 currently");
#else
                // handle enums specially
                newGrid = Activator.CreateInstance(propertyGridType, cursor, requestingWindow,
                    requestingListDisplayWindow.Items.IndexOf(requestingListDisplayWindow.GetFirstHighlightedItem())) as PropertyGrid;
#endif
#endif
            }

            #endregion

            #region Else if the object has an associated GUI element

            else if (PropertyGrid.sPropertyGridTypesForObjectType.ContainsKey(objectType))
            {
#if XBOX360
                throw new NotImplementedException("PropertyGrid creation not supported on 360.");
#else
                if (objectType.IsValueType || objectType == typeof(string))
                {

                    newGrid = Activator.CreateInstance(
                        PropertyGrid.sPropertyGridTypesForObjectType[objectType], cursor, requestingWindow,
                        requestingListDisplayWindow.Items.IndexOf(requestingListDisplayWindow.GetFirstHighlightedItem())) as PropertyGrid;
                }
                else
                {
                    Type objectDisplayerType = PropertyGrid.sPropertyGridTypesForObjectType[objectType];

                    ConstructorInfo ci = objectDisplayerType.GetConstructor(System.Type.EmptyTypes);

                    if (ci != null)
                    {
                        // The IObjectDisplayer has a no-argument constructor
                        objectToReturn = Activator.CreateInstance(objectDisplayerType) as IObjectDisplayer;
                    }
                    else
                    {
                        // The constructor requires arguments, meaning it's probabaly a Window, so let's pass a cursor
                        objectToReturn = Activator.CreateInstance(objectDisplayerType, cursor) as IObjectDisplayer;
                    }





                    if (objectToReturn is PropertyGrid)
                        newGrid = objectToReturn as PropertyGrid;
                    else if (objectToReturn is ListDisplayWindow)
                        newListDisplayWindow = objectToReturn as ListDisplayWindow;
                    
                }
#endif
            }

            #endregion

            #region Else if the object is an IEnumerable

            else if (IsIEnumerable(objectType))
            {
                ListDisplayWindow listDisplayWindow = CreateListDisplayWindowForObject(displayedObject, cursor);

                newListDisplayWindow = listDisplayWindow;
            }

            #endregion

            #region Else if the object is a value type

            else if (objectType.IsValueType)
            {
#if SILVERLIGHT
                throw new NotImplementedException();
#else
                propertyGridType = typeof(StructReferencePropertyGrid<>).MakeGenericType(objectType);

#if XBOX360
                throw new NotImplementedException("No PropertyGrid support on 360");
#else
                if (requestingListDisplayWindow != null)
                {

                    newGrid = Activator.CreateInstance(propertyGridType, cursor, requestingListDisplayWindow,
                        requestingListDisplayWindow.Items.IndexOf(requestingListDisplayWindow.GetFirstHighlightedItem())) as PropertyGrid;
                }
                else
                {
                    newGrid = Activator.CreateInstance(
                        propertyGridType, 
                        cursor, 
                        requestingPropertyGrid,
                        memberName) as PropertyGrid;

                }

#endif
                newGrid.ExcludeStaticMembers();
#endif
            }

            #endregion

            #region Else, just create a regular PropertyGrid

            else
            {
#if SILVERLIGHT
                throw new NotImplementedException();
#else
                propertyGridType = typeof(PropertyGrid<>).MakeGenericType(objectType);

#if XBOX360
                throw new NotImplementedException("No PropertyGrid support on 360");
#else

                newGrid = Activator.CreateInstance(propertyGridType, cursor) as PropertyGrid;
#endif
#endif
            }

            #endregion

            #endregion


            #region Modify the new object based off of settings set at the user level

#if !SILVERLIGHT
            if (PropertyGrid.sPropertyGridMemberSettings.ContainsKey(objectType))
            {
                PropertyGridMemberSettings memberSettings =
                    PropertyGrid.sPropertyGridMemberSettings[objectType];

                foreach (string member in memberSettings)
                {
                    newGrid.ExcludeMember(member);
                }
            }
#endif

            Window newWindow = null;

            if (newGrid != null)
            {
                newWindow = newGrid;
            }
            else if (newListDisplayWindow != null)
            {
                newWindow = newListDisplayWindow;
            }
            else if (objectToReturn is Window)
            {
                newWindow = objectToReturn as Window;
            }

#if !SILVERLIGHT
            if (PropertyGrid.sNewWindowCallbacks.ContainsKey(objectType))
            {
                PropertyGrid.sNewWindowCallbacks[objectType](newWindow);
            }
            if (PropertyGrid.sNewWindowCallbacksByTypeAsString.ContainsKey(objectType.FullName))
            {
                PropertyGrid.sNewWindowCallbacksByTypeAsString[objectType.FullName](newWindow);
            }
#endif

            if (AnyNewWindowCreated != null)
            {
                ObjectDisplayManager.AnyNewWindowCreated(newWindow);
            }

            #endregion


            if (newGrid != null)
            {
#if SILVERLIGHT 
                throw new NotImplementedException();
#else
                newGrid.ContentManagerName = ContentManagerName;
                return newGrid;
#endif
            }
            else if (newListDisplayWindow != null)
            {
#if SILVERLIGHT
                throw new NotImplementedException();
#else

                newListDisplayWindow.ContentManagerName = ContentManagerName;
                return newListDisplayWindow;
#endif
            }
            else
            {
                // This is probably a custom UI element created by the user, so we'll let it slide :)
                return objectToReturn;
            }
        }


        #endregion

        #region Private Methods

        public static bool IsIEnumerable(Type typeToTest)
        {
            Type[] interfaces = typeToTest.GetInterfaces();

            foreach (Type type in interfaces)
            {
                if (type == typeof(IEnumerable))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #endregion

    }
}
