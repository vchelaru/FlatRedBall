using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using FlatRedBall.Instructions.Reflection;

namespace FlatRedBall.Gui
{
    #region Struct

    #region XML Docs
    /// <summary>
    /// Settings which can be used to set the visibility of member elements
    /// depending on the value of another member.
    /// </summary>
    /// <remarks>
    /// This enum is used by MemberVisibleCondition which is used in the 
    /// PropertyGrid's SetConditionalMemberVisibility method to set how the
    /// state of one property can impact the visibility of another property.
    /// </remarks>
    #endregion
    public enum VisibilitySetting
    {
        IncludeOnTrue = 1,
        IncludeOnFalse = 2,
        ExcludeOnTrue = 4,
        ExcludeOnFalse = 8
    }

    struct MemberVisibleCondition
    {
        public MemberCondition MemberCondition;
        public VisibilitySetting VisibilitySettings;
        public string MemberToAffect;

        public MemberVisibleCondition(MemberCondition memberCondition, 
            VisibilitySetting visibilitySetting,
            string memberToAffect)
        {
            MemberCondition = memberCondition;
            VisibilitySettings = visibilitySetting;
            MemberToAffect = memberToAffect;
        }
    }

    #endregion

    #region XML Docs
    /// <summary>
    /// Base non-generic class for the generic PropertyWindowAssociation class.
    /// </summary>
    /// <remarks>
    /// Contains information about an element in the PropertyGrid.  Information includes the
    /// UI element displaying the property, its associated Label, the member name, read/write
    /// permissions, and category.
    /// </remarks>
    #endregion
    public class PropertyWindowAssociation
    {
        #region Fields

        #region XML Docs
        /// <summary>
        /// The window that allows for editing of the property.
        /// </summary>
        /// <remarks>
        /// Fore example, a bool property would have a ComboBox.
        /// </remarks>
        #endregion
        internal IWindow Window;
#if !SILVERLIGHT
        internal TextDisplay Label;
#endif
        internal Type Type;
        internal string MemberName;

        internal bool CanRead;
        internal bool CanWrite;
        internal MemberTypes ReferencedMemberType;

        internal List<MemberVisibleCondition> mMemberVisibleConditions = 
            new List<MemberVisibleCondition>();

        #region XML Docs
        /// <summary>
        /// When a property is not a base type understood by
        /// the PropertyGrid it is represented by a button.  These
        /// properties can then be viewed in their own PropertyGrid.
        /// Even if CanWrite is false on these properties the user should
        /// still be able to view them.  If CanWrite is false but this is 
        /// true then the UI (button) representing this property will be enabled.
        /// </summary>
        #endregion
        internal bool IsViewable;

        internal string Category = "";

        #region XML Docs
        /// <summary>
        /// Raised whenever a property is changed through the PropertyGrid.
        /// </summary>
        #endregion
        internal event FlatRedBall.Gui.GuiMessage ChangeEvent;

        internal bool IsStatic = false;

        #region XML Docs
        /// <summary>
        /// The window that allows for more detailed editing of an object.
        /// This window will appear if the user clicks on an "Edit Property" button.
        /// </summary>
        /// <remarks>
        /// Some types like ints and floats have natural representations, but other custom
        /// types do not.  Therefore if a member that the PropertyGrid does not how to display
        /// is included, then the PropertyGrid will show a button that says "Edit Property".  Pressing
        /// this button will bring up a new window which will show the details of an object in a new PropertyGrid
        /// or ListBox.  This will hold the reference to the window.
        /// 
        /// Holding the reference not only eliminates some garbage collection and memory allocation, but it also allows
        /// the user to get a reference to it to add events for custom behavior.
        /// </remarks>
        #endregion
        internal Window ChildFloatingWindow;

        #endregion

        #region Properties

        internal virtual bool IsTypedPropertyWindowAssociation
        {
            get { return false; }
        }

        #endregion

        #region Event Methods

#if !SILVERLIGHT
        // This is made internal so that the PropertyGrid can raise this continually so that
        // window visiblity is purely reactive
        internal void IncludeAndExcludeAccordingToValue(PropertyGrid propertyGrid)
        {
            for (int i = 0; i < mMemberVisibleConditions.Count; i++)
            {
                MemberVisibleCondition mvc = mMemberVisibleConditions[i];

                // Update the SelectedObject
                mvc.MemberCondition.SelectedObjectAsObject = propertyGrid.GetSelectedObject();

                
                bool result = mvc.MemberCondition.Result;

                #region If the member condition is true

                if (result)
                {
                    if ((mvc.VisibilitySettings & VisibilitySetting.ExcludeOnTrue) ==
                        VisibilitySetting.ExcludeOnTrue)
                    {
                        propertyGrid.ExcludeMember(mvc.MemberToAffect);
                    }

                    if ((mvc.VisibilitySettings & VisibilitySetting.IncludeOnTrue) ==
                        VisibilitySetting.IncludeOnTrue)
                    {
                        if (string.IsNullOrEmpty(this.Category))
                        {
                            propertyGrid.IncludeMember(mvc.MemberToAffect);
                        }
                        else
                        {
                            propertyGrid.IncludeMember(mvc.MemberToAffect, Category);
                        }
                    }
                }

                #endregion

                #region Else if the member condition is false

                else // result == false
                {
                    if ((mvc.VisibilitySettings & VisibilitySetting.ExcludeOnFalse) ==
                        VisibilitySetting.ExcludeOnFalse)
                    {
                        propertyGrid.ExcludeMember(mvc.MemberToAffect);
                    }

                    if ((mvc.VisibilitySettings & VisibilitySetting.IncludeOnFalse) ==
                        VisibilitySetting.IncludeOnFalse)
                    {
                        if (string.IsNullOrEmpty(this.Category))
                        {
                            propertyGrid.IncludeMember(mvc.MemberToAffect);
                        }
                        else
                        {
                            propertyGrid.IncludeMember(mvc.MemberToAffect, Category);
                        }
                    }

                }

                #endregion
            }
        }
#endif
        #endregion

        #region Methods

#if !SILVERLIGHT
        internal PropertyWindowAssociation(IWindow window, TextDisplay label, 
            Type type, string memberName, bool canRead, bool canWrite, MemberTypes memberType, bool isStatic)
        {

            Window = window; Label = label;  Type = type; MemberName = memberName;

            CanRead = canRead;
            CanWrite = canWrite;

            ReferencedMemberType = memberType;

            IsStatic = isStatic;
        }
#endif
        internal void ClearChangeEvent()
        {
            ChangeEvent = null;
        }

        public virtual void SetPropertyFor(PropertyGrid propertyGrid, object objectToSet)
        {
        }
#if !SILVERLIGHT
        public void OnMemberChanged(IWindow callingWindow, PropertyGrid parentPropertyGrid)
        {
            if (ChangeEvent != null)
            {
                ChangeEvent((Window)callingWindow);
            }



            IncludeAndExcludeAccordingToValue(parentPropertyGrid);
        }
#endif
        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append("Name: ").AppendLine(MemberName + " ");
            stringBuilder.Append("Category: ").AppendLine(Category);

            return stringBuilder.ToString();
        }

        #endregion
    }

    #region XML Docs
    /// <summary>
    /// Generic class storing associations between UI elements and settings for the member
    /// in the PropertyGrid.
    /// </summary>
    /// <remarks>
    /// <seealso cref="FlatRedBall.Gui.PropertyWindowAssociation"/>
    /// </remarks>
    /// <typeparam name="T">The type of the member being shown.</typeparam>
    #endregion
    public class PropertyWindowAssociation<T> : PropertyWindowAssociation
    {
        #region Properties

        internal override bool IsTypedPropertyWindowAssociation
        {
            get
            {
                return true ;
            }
        }

        #endregion

        #region Methods

#if !SILVERLIGHT
        public PropertyWindowAssociation(Window window, TextDisplay label,
            Type type, string memberName, bool canRead, bool canWrite, 
            MemberTypes memberType, bool isStatic) : 
            base(window, label, type, memberName, canRead, 
            canWrite, memberType, isStatic)
        {

        }

        public override void SetPropertyFor(PropertyGrid propertyGrid, object objectToSet)
        {
            PropertyGrid<T> typedGrid = propertyGrid as PropertyGrid<T>;
            T typedObject = (T)objectToSet;
            if (typedGrid != null)
            {
                typedGrid.SelectedObject = typedObject;
            }
            else
            {
                throw new Exception("PropertyGrid type and object type do not match");
            }
        }
#endif
        #endregion
    }
}
