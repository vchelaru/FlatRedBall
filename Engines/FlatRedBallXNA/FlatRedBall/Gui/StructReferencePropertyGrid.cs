using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Gui
{
    #region XML Docs
    /// <summary>
    /// A class that can be used to apply changes to fields and properties of an object.
    /// </summary>
    /// <remarks>
    /// If an object has a Vector3 field, creating a regular PropertyGrid for that Vector3
    /// and changing that Vector3 will not actually change the field's value.  Since C# doesn't
    /// have pointers, then the only way to change the source's field is to keep a reference to
    /// the object that has the field, then push changes through that instance.
    /// 
    /// The StructReferencePropertyGrid simulates a pointer by storing reference to the object that
    /// contains the field.
    /// </remarks>
    /// <typeparam name="T">The type of object that the PropertyGrid stores.</typeparam>
    #endregion
    public class StructReferencePropertyGrid<T>
#if !SILVERLIGHT
        : PropertyGrid<T>// where T : struct
#endif
    {
        #region Fields
        PropertyGrid mPropertyGridOfObject;
        ListDisplayWindow mListDisplayWindowOfObject;
        
        // If mIndexOfProperty is -1 (default) then the 
        // mNameOfProperty is used.  Otherwise, the index
        // is set.
        string mNameOfProperty;

        int mIndexOfObject = -1;

        #endregion

        #region Properties

        public string NameOfProperty
        {
            get { return mNameOfProperty; }
        }

        public override bool IsWindowOrChildrenReceivingInput
        {
            get
            {
                bool returnValue = false;
                foreach (Window window in mChildren)
                {
                    returnValue |= window.IsWindowOrChildrenReceivingInput;
                }

                return returnValue;
            }
        }

        #endregion

        #region Event Methods

        protected void UpdateObject(Window callingWindow)
        {
#if WINDOWS_PHONE || SILVERLIGHT || MONODROID
            throw new NotImplementedException();
#else
            if (mIndexOfObject != -1)
            {
                mListDisplayWindowOfObject.SetSelectedObjectsObjectAtIndex(mIndexOfObject, mSelectedObject);
            }
            else
            {
                mPropertyGridOfObject.SetSelectedObjectsMember(mNameOfProperty, mSelectedObject);
            }
#endif
        }

        #endregion

        #region Methods

        public StructReferencePropertyGrid(Cursor cursor, ListDisplayWindow windowOfObject, int indexOfObject)
            : base(cursor)
        {
            mListDisplayWindowOfObject = windowOfObject;
            mIndexOfObject = indexOfObject;

            foreach (PropertyWindowAssociation pwa in mPropertyWindowAssociations)
            {
                pwa.ChangeEvent += UpdateObject;
            }
        }


        public StructReferencePropertyGrid(Cursor cursor, PropertyGrid propertyGridOfObject, string nameOfProperty)
            : base(cursor)
        {
            mPropertyGridOfObject = propertyGridOfObject;
            mNameOfProperty = nameOfProperty;

            foreach (PropertyWindowAssociation pwa in mPropertyWindowAssociations)
            {
                pwa.ChangeEvent += UpdateObject;
            }
        }

        public override IWindow IncludeMember(string propertyToInclude)
        {
            IWindow window = base.IncludeMember(propertyToInclude);

            PropertyWindowAssociation pwa =
                mPropertyWindowAssociations[this.IndexOfWindow(window)];

            pwa.ChangeEvent += UpdateObject;

            return window;
        }

        #endregion

    }
}
