using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
#if FRB_MDX
using Microsoft.DirectX.Direct3D;
using Vector2 = Microsoft.DirectX.Vector2;
using Vector3 = Microsoft.DirectX.Vector3;
using Texture2D = FlatRedBall.Texture2D;
using System.Drawing;
using Microsoft.DirectX;
#elif FRB_XNA
using Vector2 = Microsoft.Xna.Framework.Vector2;
using Microsoft.Xna.Framework.Graphics;

using Microsoft.Xna.Framework;
#endif

using FlatRedBall.Instructions;
using FlatRedBall.Graphics;

using FlatRedBall.Content.Saves;
using FlatRedBall.Instructions.Reflection;
using FlatRedBall.Input;



namespace FlatRedBall.Gui
{
#if !SILVERLIGHT
    #region BitmapFontChangePropertyGrid

    public class BitmapFontChangePropertyGrid : PropertyGrid<BitmapFontSave>
    {
        #region Fields

        object mMemberOwner;
        string mMemberName;

        const string FromFntString = "<FROM .FNT>";

        #endregion

        public BitmapFont LastBitmapFont
        {
            get;
            private set;
        }

        #region Events

        public event WindowEvent OkClick;

        #endregion

        #region Event Methods

        private void SetTextureEvent(Window callingWindow)
        {
            SelectedObject.TextureFileName = ((FileTextBox)callingWindow).Text;
        }

        private void SetFontEvent(Window callingWindow)
        {
            SelectedObject.FontFileName = ((FileTextBox)callingWindow).Text;
            SelectedObject.TextureFileName = "<FROM .FNT>";
            UpdateDisplayedProperties();
        }

        private void SetParentBitmapFont(Window callingWindow)
        {
            if (string.IsNullOrEmpty(ContentManagerName))
            {
                throw new InvalidOperationException("The Bitmap Font PropertyGrid has a null ContentManagerName");

            }
#if !WINDOWS_PHONE && !MONODROID
            else if (string.IsNullOrEmpty(SelectedObject.TextureFileName))
            {
                OkCancelWindow okCancelWindow = GuiManager.ShowOkCancelWindow("Cannot create BitmapFont because a texture file has not been selected.", "Error creating BitmapFont");
                return;
            }
            else if (string.IsNullOrEmpty(SelectedObject.FontFileName))
            {
                OkCancelWindow okCancelWindow = GuiManager.ShowOkCancelWindow("Cannot create BitmapFont because a .fnt file has not been selected.", "Error creating BitmapFont");
                return;
            }
#endif
            Type type = mMemberOwner.GetType();



            BitmapFont bitmapFont = null;

            if (SelectedObject.TextureFileName == FromFntString)
            {
                bitmapFont = new BitmapFont(
                    SelectedObject.FontFileName,
                    ContentManagerName);
            }

            else
            {
                bitmapFont = new BitmapFont(
                 SelectedObject.TextureFileName,
                 SelectedObject.FontFileName,
                 ContentManagerName);
            }
            object[] args = { bitmapFont };

            if (type.GetMember("Font")[0].MemberType == MemberTypes.Property)
            {
                LastBitmapFont = (BitmapFont)type.GetProperty("Font").GetValue(mMemberOwner, null);

                type.InvokeMember(mMemberName,
                    BindingFlags.SetProperty, null, mMemberOwner, args);
            }
            else // it's a field
            {
                LastBitmapFont = (BitmapFont)type.GetField("Font").GetValue(mMemberOwner);

                type.InvokeMember(mMemberName,
                    BindingFlags.SetField, null, mMemberOwner, args);
            }

            if (OkClick != null)
            {
                OkClick(this);
            }

            GuiManager.RemoveWindow(this);

        }

        #endregion

        #region Methods

        public BitmapFontChangePropertyGrid(Cursor cursor, object memberOwner, string memberName)
            : base(cursor)
        {
            mMemberOwner = memberOwner;
            mMemberName = memberName;

            Button okButton = new Button(cursor);
            okButton.ScaleX = 4;
            okButton.Text = "Ok";
            AddWindow(okButton);
            okButton.Click += SetParentBitmapFont;

            Button cancelButton = new Button(cursor);
            cancelButton.ScaleX = 4;
            cancelButton.Text = "Cancel";
            AddWindow(cancelButton);
            cancelButton.Click += GuiManager.RemoveParentOfWindow;

            SelectedObject = new BitmapFontSave();

            BitmapFont parentMemberBitmapFont = null;

            if(memberOwner is Text)
            {
                parentMemberBitmapFont = ((Text)memberOwner).Font;
            }

            FileTextBox fontName = new FileTextBox(cursor);
            fontName.ScaleX = 11;
            fontName.SetFileType("fnt");
            this.ReplaceMemberUIElement("FontFileName", fontName);
            fontName.FileSelect += SetFontEvent;

            FileTextBox textureName = new FileTextBox(cursor);
            textureName.ScaleX = 11;
            textureName.SetFileType("graphic");
            this.ReplaceMemberUIElement("TextureFileName", textureName);
            textureName.FileSelect += SetTextureEvent;
            
            
            this.MoveWindowToTop(fontName);


            if(parentMemberBitmapFont != null)
            {
                SelectedObject.TextureFileName = parentMemberBitmapFont.TextureName;
                SelectedObject.FontFileName = parentMemberBitmapFont.FontFile;

                textureName.Text = parentMemberBitmapFont.TextureName;
                fontName.Text = parentMemberBitmapFont.FontFile;


            }
        }

        #endregion
    }

    #endregion

#endif

    #region base abstract PropertyGrid class
    public abstract class PropertyGrid : Window, IObjectDisplayer
    {
        #region Fields

        public static bool SortEnumerations = false;

        #region XML Docs
        /// <summary>
        /// The PropertyGrid can be tied to a list of InstructionLists.
        /// These hold the instructions necessary to undo the changes made
        /// by the PropertyGrid.
        /// 
        /// It is common to store a list of lists so that multiple changes
        /// can be perfomred during one undo.  In other words, if X and Y
        /// were changed in one action then the instructions for changing the
        /// X and Y back will be in one InstructionList.
        /// </summary>
        #endregion
        protected List<InstructionList> mUndoInstructions;

        protected Type mSelectedType;

        protected List<PropertyWindowAssociation> mPropertyWindowAssociations =
            new List<PropertyWindowAssociation>();

#if !SILVERLIGHT


        //protected List<PropertyWindowAssociation> mExtraChildrenWindows = new List<PropertyWindowAssociation>();

        protected string mContentManagerName;

        static internal Dictionary<Type, GuiMessage> sNewWindowCallbacks = new Dictionary<Type, GuiMessage>();
        static internal Dictionary<string, GuiMessage> sNewWindowCallbacksByTypeAsString = new Dictionary<string, GuiMessage>();


        #region XML Docs
        /// <summary>
        /// Associated type with list of strings of members to exclude for a given type.  This is used
        /// when children PropertyGrids are created.
        /// </summary>
        #endregion
        static internal Dictionary<Type, PropertyGridMemberSettings> sPropertyGridMemberSettings =
            new Dictionary<Type, PropertyGridMemberSettings>();


        protected WindowArrayVisibilityListBox mWavListBox;
#endif

        static internal Dictionary<Type, Type> sPropertyGridTypesForObjectType =
            new Dictionary<Type, Type>();

        protected static float mNewTextBoxScale = 6.5f;

        internal string mUncategorizedCategoryName = "Uncategorized";


        #endregion

        #region Properties

        object IObjectDisplayer.ObjectDisplayingAsObject
        {
            get
            {
                return GetSelectedObject();
            }
            set
            {
                SetSelectedObject(value);
            }
        }

#if !SILVERLIGHT
        #region XML Docs
        /// <summary>
        /// Gets and sets the content mananager name that is used when loading assets like Textures.
        /// </summary>
        #endregion
        public string ContentManagerName
        {
            get { return mContentManagerName; }
            set
            {
                mContentManagerName = value;
                // loop through any children PropertyGrids and set their ContentManagerName to
                // be the same

                foreach (PropertyWindowAssociation pwa in mPropertyWindowAssociations)
                {
                    if (pwa.Window is PropertyGrid)
                    {
                        ((PropertyGrid)pwa.Window).ContentManagerName = value;
                    }
                }
            }
        }


        public virtual List<InstructionList> UndoInstructions
        {
            set 
            { 
                mUndoInstructions = value;

                foreach (PropertyWindowAssociation pwa in mPropertyWindowAssociations)
                {
                    #region ListDisplayWindow

                    if (pwa.Window is ListDisplayWindow)
                    {
                        ((ListDisplayWindow)pwa.Window).UndoInstructions = mUndoInstructions;

                    }

                    #endregion

                    #region PropertyGrid

                    else if (pwa.Window is PropertyGrid)
                    {
                        ((PropertyGrid)pwa.Window).UndoInstructions = mUndoInstructions;
                    }

                    #endregion

                }            
            }
        }


        public WindowArrayVisibilityListBox WavListBox
        {
            get { return mWavListBox; }
        }


        public static float NewTextBoxScale
        {
            get { return mNewTextBoxScale; }
            set { mNewTextBoxScale = value; }
        }


        public override bool Visible
        {
            get
            {
                return base.Visible;
            }
            set
            {
                if (value != base.mVisible)
                {

                    base.Visible = value;
                    if (Visible)
                    {
                        UpdateDisplayedProperties();
                    }
                }
                
            }
        }


        public string SelectedCategory
        {
            get
            {
                if (mWavListBox != null)
                {
                    return mWavListBox.GetFirstHighlightedItem().Text;
                }
                else
                {
                    return null;
                }
            }
        }


        public Type SelectedType
        {
            get { return mSelectedType; }
        }
#endif
        #endregion

        #region Events

        public event GuiMessage AfterUpdateDisplayedProperties;

        #endregion

        #region Event Methods

        protected abstract void ExtraWindowResize(Window callingWindow);

        #endregion

        #region Methods

        #region Constructor

        internal PropertyGrid(Cursor cursor)
            : base(cursor)
        { }

        #endregion

        #region Public Methods

#if !SILVERLIGHT

        #region XML Docs
        /// <summary>
        /// Adds an always-displayed Window to the PropertyGrid.  This window will appear
        /// after all member-displaying Windows and will automatically be positioned appropriately.
        /// </summary>
        /// <param name="windowToAdd">The window to add.</param>
        #endregion
        public override void AddWindow(IWindow windowToAdd)
        {
            AddWindow(windowToAdd, "");
        }


        public void AddWindow(IWindow windowToAdd, string category)
        {
            base.AddWindow(windowToAdd);

            if (!string.IsNullOrEmpty(category))
            {
                CreateWavListBoxIfNecessary();
            }

            if (category == mUncategorizedCategoryName)
            {
                category = "";
            }

            if (string.IsNullOrEmpty(category) == false && mWavListBox.Contains(category) == false)
            {
                mWavListBox.AddWindowArray(category, new WindowArray());
            }

            PropertyWindowAssociation propertyWindowAssociation = new PropertyWindowAssociation(
                windowToAdd, null, null, 
                null, // member name should stay null 
                true, true, MemberTypes.All, false);
            propertyWindowAssociation.Category = category;

            //mExtraChildrenWindows.Add(propertyWindowAssociation);
            mPropertyWindowAssociations.Add(propertyWindowAssociation);

            if (windowToAdd is Window)
            {
                ((Window)windowToAdd).Resizing += this.ExtraWindowResize;
            }

            UpdateScaleAndWindowPositions();

            if (mWavListBox != null)
            {
                UpdateWavMembership();
            }
        }


        public abstract void ExcludeAllMembers();


        public abstract void ExcludeMember(string propertyToExclude);


        public abstract void ExcludeMembersInType(Type typeContainingMembersToExclude);


        public abstract void ExcludeStaticMembers();


        public abstract string GetMemberNameForUIElement(Window window);

#endif

        public Type GetSelectedObjectType()
        {
            return this.GetType().GetGenericArguments()[0];
        }


        
        public abstract IWindow GetUIElementForMember(string memberName);
        
        
        public abstract IWindow IncludeMember(string propertyToInclude);
        
        
        public abstract IWindow IncludeMember(string propertyToInclude, string category);

#if !SILVERLIGHT

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


        public void MakeMemberReadOnly(string memberName)
        {
            MakeMemberReadOnly(memberName, true);
        }


        public void MakeMemberReadOnly(string memberName, bool callUpdateDisplayProperties)
        {
            PropertyWindowAssociation pwa = GetPropertyWindowAssociationForMember(memberName);

            if (pwa != null)
            {
                pwa.CanWrite = false;
            }

            if (callUpdateDisplayProperties)
            {
                UpdateUIAndDisplayedProperties();
            }
            else
            {
                UpdateUI();
            }
        }


        public void MakeMemberWritable(string memberName)
        {
            MakeMemberWritable(memberName, true);
        }


        public void MakeMemberWritable(string memberName, bool callUpdateDisplayProperties)
        {            
            PropertyWindowAssociation pwa = GetPropertyWindowAssociationForMember(memberName);

            if (pwa != null)
            {
                pwa.CanWrite = true;
            }

            if (callUpdateDisplayProperties)
            {
                UpdateUIAndDisplayedProperties();
            }
            else
            {
                UpdateUI();
            }
        }


        public void MoveWindowToTop(Window window)
        {
            PropertyWindowAssociation pwa = GetPwaForWindow(window);

            if (mPropertyWindowAssociations.Contains(pwa))
            {
                mPropertyWindowAssociations.Remove(pwa);
                mPropertyWindowAssociations.Insert(0, pwa);
                UpdateUIAndDisplayedProperties();
            }
        }

        public void MoveWindowToBottom(Window window)
        {
            PropertyWindowAssociation pwa = GetPwaForWindow(window);

            if (mPropertyWindowAssociations.Contains(pwa))
            {
                mPropertyWindowAssociations.Remove(pwa);
                mPropertyWindowAssociations.Add(pwa);
                UpdateUIAndDisplayedProperties();
            }

        }

        public void RemoveCategory(string categoryToRemove)
        {
            if (mWavListBox != null)
            {
                if (mWavListBox.Contains(categoryToRemove))
                {
                    mWavListBox.RemoveItemByName(categoryToRemove);

                    foreach (PropertyWindowAssociation pwa in mPropertyWindowAssociations)
                    {
                        if (pwa.Category == categoryToRemove)
                        {
                            pwa.Category = mUncategorizedCategoryName;
                        }
                    }

                    UpdateWavMembership();

                    UpdateScaleAndWindowPositions();
                }
                RemoveWavListBoxIfNecessary();

            }
            else
            {
                throw new InvalidOperationException("Can't remove the category " + categoryToRemove + " because there are no categories to remove yet. " +
                    "Categories must first be added to switch the PropertyGrid into \"category mode\"");
            }
        }


        public override void RemoveWindow(IWindow windowToRemove)
        {
            base.RemoveWindow(windowToRemove);

            for(int i = mPropertyWindowAssociations.Count - 1; i > -1; i--)
            {
                PropertyWindowAssociation pwa = mPropertyWindowAssociations[i];

                if (pwa.Window == windowToRemove)
                {
                    mPropertyWindowAssociations.RemoveAt(i);
                }
            }

            UpdateScaleAndWindowPositions();

            if (mWavListBox != null)
            {
                UpdateWavMembership();
            }
        }


        public void RemoveWindowBase(IWindow windowToRemove)
        {
            base.RemoveWindow(windowToRemove);
        }


        public abstract void ReplaceMemberUIElement(string propertyName, IWindow newUIElement);


        public void ReplaceListUIWithListDisplayWindow()
        {
            foreach (PropertyWindowAssociation pwa in mPropertyWindowAssociations)
            {
                
                if (ObjectDisplayManager.IsIEnumerable(pwa.Type))
                {
                    object memberValue = GetSelectedObjectsMember(pwa.MemberName);

                    ListDisplayWindow listDisplayWindow = 
                        ObjectDisplayManager.CreateListDisplayWindowForObject(memberValue, mCursor);

                    listDisplayWindow.HasCloseButton = false;
                    listDisplayWindow.HasMoveBar = false;
                    listDisplayWindow.DrawOuterWindow = false;

                    ReplaceMemberUIElement(
                        pwa.MemberName,
                        listDisplayWindow);

                    if (string.IsNullOrEmpty(pwa.Category))
                    {
                        IncludeMember(pwa.MemberName, pwa.MemberName);
                        SetMemberDisplayName(pwa.MemberName, "");
                    }
                }

            }

        }


        public void SelectCategory(string categoryToSelect)
        {
            mWavListBox.HighlightItem(categoryToSelect);
        }

        #region XML Docs
        /// <summary>
        /// Sets the event to raise when a particular property is changed through the PropertyGrid.
        /// </summary>
        /// <remarks>
        /// A property cannot have multiple ChangeEvents.  The reason for this is so that the user
        /// can reassign the same ChangeEvent without fear of duplicating calls.  It is common when
        /// selecting an object to exclude some properties, include others, and set the callbacks.
        /// </remarks>
        /// <param name="property">The name of the property</param>
        /// <param name="changeEvent">The event to raise when the property changes.</param>
        #endregion
        public void SetMemberChangeEvent(string member, GuiMessage changeEvent)
        {
            PropertyWindowAssociation pwa = GetPropertyWindowAssociationForMember(member);

            if (pwa == null)
                throw new System.ArgumentException("Could not find property " + member + ".  Check if name is valid or if property has been excluded.");

            pwa.ChangeEvent += changeEvent;
        }


        public abstract void SetConditionalMemberVisibility<MemberType>(
            string memberToTest,
            Operator operatorToUse,
            MemberType value,
            VisibilitySetting visibilitySetting,
            string memberToIncludeOrExclude) where MemberType : IComparable;


        public abstract void SetOptionsForMember(string member, IList<string> availableOptions);


        public abstract void SetOptionsForMember(string member, IList<string> availableOptions, bool forceOptions);


        public abstract void SetOptionsForMember(string member, IDictionary<string, object> dict);


        public abstract void SetOptionsForMember(string member, ICollection collection, StringRepresentation rep);


        public static void SetCategoryScaleX(PropertyGrid pg, float newScaleX)
        {
            pg.WavListBox.ScaleX = newScaleX;
            pg.UpdateScaleAndWindowPositions();
        }


        public void SetLabelForWindow(Window window, string displayName)
        {
            PropertyWindowAssociation pwa = GetPwaForWindow(window);

            if (pwa != null)
            {
                if (pwa.Label != null)
                {
                    pwa.Label.Text = displayName;

                }
                else
                {
                    TextDisplay textDisplay = new TextDisplay(mCursor);
                    AddWindowBase(textDisplay);
                    pwa.Label = textDisplay;
                    textDisplay.Text = displayName;
                    UpdateWavMembership(pwa);
                }
            }

            UpdateScaleAndWindowPositions();

        }


        public static void SetPropertyGridMemberSettings(Type type, PropertyGridMemberSettings propertyGridMemberSettings)
        {

            if (sPropertyGridMemberSettings.ContainsKey(type))
            {
                sPropertyGridMemberSettings[type] = propertyGridMemberSettings;
            }
            else
            {
                sPropertyGridMemberSettings.Add(type, propertyGridMemberSettings);
            }
        }
#endif

        public static void SetPropertyGridTypeAssociation(Type typeOfObject,
            Type typeOfPropertyGridToCreate)
        {
            if (PropertyGrid.sPropertyGridTypesForObjectType.ContainsKey(typeOfObject))
                PropertyGrid.sPropertyGridTypesForObjectType.Remove(typeOfObject);
            PropertyGrid.sPropertyGridTypesForObjectType.Add(typeOfObject, typeOfPropertyGridToCreate);
        }

#if !SILVERLIGHT
        #region XML Docs
        /// <summary>
        /// Sets an event to be raised when a new PropertyGrid or ListDisplayWindow is created.
        /// </summary>
        /// <param name="type">The type of the object to associate the new callback with.  In other words, 
        /// when an object of this type is being viewed through the creation of a new window the message argument
        /// will be reaised.</param>
        /// <param name="message">The GuiMessage to raise on creation.  The argument passed to this message is the newly-created
        /// window.</param>
        #endregion
        public static void SetNewWindowEvent<ObjectDisplayedType>(GuiMessage message)
        {
            SetNewWindowEvent(typeof(ObjectDisplayedType), message);
        }

        #region XML Docs
        /// <summary>
        /// Sets an event to be raised when a new PropertyGrid or ListDisplayWindow is created.
        /// </summary>
        /// <param name="type">The type of the object to associate the new callback with.  In other words, 
        /// when an object of this type is being viewed through the creation of a new window the message argument
        /// will be reaised.</param>
        /// <param name="message">The GuiMessage to raise on creation.  The argument passed to this message is the newly-created
        /// window.</param>
        #endregion
        public static void SetNewWindowEvent(Type objectDisplayedType, GuiMessage message)
        {
            if (sNewWindowCallbacks.ContainsKey(objectDisplayedType))
            {
                sNewWindowCallbacks[objectDisplayedType] = message;
            }
            else
            {
                sNewWindowCallbacks.Add(objectDisplayedType, message);
            }
        }


        public static void SetNewWindowEvent(string typeFullName, GuiMessage message)
        {
            if (sNewWindowCallbacksByTypeAsString.ContainsKey(typeFullName))
            {
                sNewWindowCallbacksByTypeAsString[typeFullName] = message;
            }
            else
            {
                sNewWindowCallbacksByTypeAsString.Add(typeFullName, message);
            }
        }

#endif

        public abstract void SetMemberDisplayName(string member, string displayName);

        internal abstract void UpdateUI();


        public void UpdateToObject()
        {
            UpdateDisplayedProperties();
        }


        public abstract void UpdateDisplayedProperties();

        #endregion

        #region Internal Methods
#if !SILVERLIGHT

        internal void AddWindowBase(IWindow windowToAdd)
        {
            base.AddWindow(windowToAdd);
        }


        internal void CallAfterUpdateDisplayedProperties()
        {
            if (AfterUpdateDisplayedProperties != null)
            {
                AfterUpdateDisplayedProperties(this);
            }
        }


        internal void CreateWavListBoxIfNecessary()
        {
            if (mWavListBox == null)
            {
                mWavListBox = new WindowArrayVisibilityListBox(mCursor);
                AddWindowBase(mWavListBox);

                mWavListBox.CurrentToolTipOption = ListBoxBase.ToolTipOption.CursorOver;
                mWavListBox.ScaleX = 5;
                mWavListBox.X = 5.5f;
                this.ScaleX += 5f;

                mWavListBox.AddWindowArray(mUncategorizedCategoryName, new WindowArray());

                // If there are any PropertyWindowArrays that have "" as their category, set the category to "Uncategorized"
                foreach (PropertyWindowAssociation pwa in this.mPropertyWindowAssociations)
                {
                    if (string.IsNullOrEmpty(pwa.Category))
                    {
                        pwa.Category = mUncategorizedCategoryName;
                    }
                }
            }
        }


        internal override void DrawSelfAndChildren(Camera camera)
        {
            base.DrawSelfAndChildren(camera);
        }

        internal BindingFlags GetGetterBindingFlag(PropertyWindowAssociation pwa)
        {
            switch (pwa.ReferencedMemberType)
            {
                case MemberTypes.Field:
                    return BindingFlags.GetField;
                case MemberTypes.Property:
                    return BindingFlags.GetProperty;
                default:
                    return BindingFlags.GetProperty;
            }
        }


        // for debugging only.  Can remove this later
        internal override int GetNumberOfVerticesToDraw()
        {
            return base.GetNumberOfVerticesToDraw();
        }

 
        internal PropertyWindowAssociation GetPwaForWindow(Window window)
        {
            foreach (PropertyWindowAssociation pwa in mPropertyWindowAssociations)
            {
                if (pwa.Window == window)
                {
                    return pwa;
                }
            }

            return null;
        }
#endif

        internal abstract object GetSelectedObject();

#if !SILVERLIGHT

        //internal int IndexOfProperty(string propertyName)
        //{
        //    for (int i = 0; i < mPropertyWindowAssociations.Count; i++)
        //    {
        //        if (mPropertyWindowAssociations[i].MemberName == propertyName)
        //            return i;
        //    }
        //    return -1;
        //}

#endif
        #region XML Docs
        /// <summary>
        /// Sets the SelectedObject property of the PropertyGrid.  This allows
        /// code that has reference to a base PropertyGrid to still set the SelectedObject.
        /// 
        /// This should never be used outside of the engine!
        /// </summary>
        /// <param name="objectToSet">The object to show.</param>
        #endregion
        internal abstract void SetSelectedObject(object objectToSet);

        internal abstract void SetSelectedObjectsMember(string memberName, object value);


        internal abstract void SetSelectedObjectsObjectAtIndex(int index, object value);


        public abstract void UpdateScaleAndWindowPositions();


        internal abstract void UpdateUIAndDisplayedProperties();


        public abstract void UpdateWavMembership();

        internal abstract void UpdateWavMembership(PropertyWindowAssociation pwa);
        #endregion

        #region Protected Methods

#if !SILVERLIGHT
        protected object GetSelectedObjectsMember(string memberName)
        {
            PropertyWindowAssociation pwa = GetPropertyWindowAssociationForMember(memberName);

            if (pwa.CanRead)
            {
                return GetSelectedObjectsMember(memberName, pwa);
            }
            else
            {
                return null;
            }
        }
#endif
        protected abstract object GetSelectedObjectsMember(string memberName, PropertyWindowAssociation pwa);


        protected void ClearAllUi()
        {
            foreach (PropertyWindowAssociation pwa in mPropertyWindowAssociations)
            {
                if (pwa.Window == null)
                {
                    continue;
                }

                if (pwa.Window is TextBox)
                {
                    ((TextBox)pwa.Window).Text = "";
                }

                else if (pwa.Window is UpDown)
                {
                    ((UpDown)pwa.Window).Clear();
                }

                else if (pwa.Window is FileTextBox)
                {
                    ((FileTextBox)pwa.Window).Text = "";
                }



            }
        }
        #endregion

        #region Private Methods
#if !SILVERLIGHT
        internal PropertyWindowAssociation GetPropertyWindowAssociationForMember(string memberName)
        {
            for (int i = 0; i < mPropertyWindowAssociations.Count; i++)
            {
                PropertyWindowAssociation pwa = mPropertyWindowAssociations[i];
                if (pwa.MemberName == memberName)
                {
                    return pwa;
                }
            }

            return null;
        }
        
        private void RemoveWavListBoxIfNecessary()
        {
            if (mWavListBox.Count < 2)
            {
                RemoveWindowBase(mWavListBox);

                mWavListBox = null;

                foreach (PropertyWindowAssociation pwa in mPropertyWindowAssociations)
                {
                    pwa.Category = "";
                }

                UpdateDisplayedProperties();
            }
        }
#endif



        #endregion


        #endregion


    }
    #endregion

#if !SILVERLIGHT

    public class PropertyGrid<T> : PropertyGrid, IObjectDisplayer<T>
    {
        #region Fields
        protected T mSelectedObject;

        // Some properties are only set
        // through a series of events.  The
        // originating UI can be lost through
        // these event calls so in this case the
        // name of the property being set is saved 
        // in this variable
        string mCachedPropertyName;

        float mDistanceToShiftWindows = 11;

        List<string> mExcludedMembers;

        Dictionary<Type, List<string>> mExcludedEnumerationValues = new Dictionary<Type, List<string>>();

		protected static float LastXPosition = 51;
		protected static float LastYPosition = 44;

        #endregion

        #region Properties

        public virtual T SelectedObject
        {
            set
            {
                if (value == null || !value.Equals(ObjectDisplaying))
                {


                    if (InputManager.ReceivingInput != null && this.IsWindowOrChildrenReceivingInput)
                    {
                        InputManager.ReceivingInput = null;
                    }

                    // Make sure to do this AFTER the receiving input is set to null
                    ClearAllUi();  
                }

                ObjectDisplaying = value;               
            }
            get
            {
                return ObjectDisplaying; 
            }
        }

        internal void SetSelectedObjectNoLoseFocus(T newSelectedObject)
        {
            ObjectDisplaying = newSelectedObject;
        }

        public float DistanceToShiftWindows
        {
            get { return mDistanceToShiftWindows; }
            set { mDistanceToShiftWindows = value; }
        }

        #endregion

        #region Event Methods

        private void ChangeBoolValue(Window callingWindow)
        {
            if (mSelectedObject != null)
            {
                int index = IndexOfWindow(callingWindow);

                // Make the undo
                if (mUndoInstructions != null)
                {
                    bool oldValue = (bool)
                        GetSelectedObjectsMember(mPropertyWindowAssociations[index].MemberName);
                    InstructionList instructionList = new InstructionList();

                    instructionList.Add(
                        new Instruction<T, bool>(mSelectedObject,
                        mPropertyWindowAssociations[index].MemberName, oldValue, 0));

                    mUndoInstructions.Add(instructionList);
                }


                bool currentValue = (bool)(((ComboBox)callingWindow).SelectedObject);

                if (index != -1)
                {
                    if (this.mSelectedType.IsValueType)
                    {
#if !XBOX360 && !WINDOWS_PHONE && !MONODROID
                        FieldInfo fieldInfo = mSelectedType.GetField(
                            mPropertyWindowAssociations[index].MemberName);

                        fieldInfo.SetValueDirect(
                            __makeref(mSelectedObject), currentValue);
#else
                        throw new NotImplementedException();
#endif
                    }
                    else
                    {
                        object[] args = { currentValue };

                        mSelectedType.InvokeMember(mPropertyWindowAssociations[index].MemberName,
                            GetSetterBindingFlag(mPropertyWindowAssociations[index]), null, mSelectedObject, args);
                    }

                    // Perform any custom behavior related to this member being changed
                    mPropertyWindowAssociations[index].OnMemberChanged(callingWindow, this);

                }
            }
        }

        private void ChangeByteValue(Window callingWindow)
        {
            int index = IndexOfWindow(callingWindow);
            byte currentValue = (byte)(((UpDown)callingWindow).CurrentValue);

            if (index != -1 && mPropertyWindowAssociations[index].CanWrite)
            {
                if (this.mSelectedType.IsValueType)
                {
#if XBOX360 || WINDOWS_PHONE || MONODROID
                    throw new NotImplementedException();
#else
                    FieldInfo fieldInfo = mSelectedType.GetField(
                        mPropertyWindowAssociations[index].MemberName);

                    fieldInfo.SetValueDirect(
                        __makeref(mSelectedObject), currentValue);
#endif
                }
                else
                {
                    object[] args = { currentValue };

                    mSelectedType.InvokeMember(mPropertyWindowAssociations[index].MemberName,
                        GetSetterBindingFlag(mPropertyWindowAssociations[index]), null, mSelectedObject, args);
                }

                // Perform any custom behavior related to this member being changed
                mPropertyWindowAssociations[index].OnMemberChanged(callingWindow, this);
            }
        }

        private void ChangeEnumValue(Window callingWindow)
        {
            int index = IndexOfWindow(callingWindow);


            if (index != -1 && mPropertyWindowAssociations[index].CanWrite)
            {
                // Make the undo
                if (mUndoInstructions != null)
                {
                    Enum oldValue =
                        GetSelectedObjectsMember(mPropertyWindowAssociations[index].MemberName) as Enum;
                    InstructionList instructionList = new InstructionList();

                    instructionList.Add(
                        new Instruction<T, Enum>(mSelectedObject,
                        mPropertyWindowAssociations[index].MemberName, oldValue, 0));

                    mUndoInstructions.Add(instructionList);
                }

                string enumAsString = ((ComboBox)callingWindow).Text;

                FieldInfo enumFieldInfo = mPropertyWindowAssociations[index].Type.GetField(enumAsString);


                if (this.mSelectedType.IsValueType)
                {
#if XBOX360 || WINDOWS_PHONE || MONODROID
                    throw new NotImplementedException();
#else

                    FieldInfo fieldInfo = mSelectedType.GetField(
                        mPropertyWindowAssociations[index].MemberName);

                    fieldInfo.SetValueDirect(
                        __makeref(mSelectedObject), enumFieldInfo.GetValue(mPropertyWindowAssociations[index].Type));
#endif
                }
                else
                {
                    object[] args = { enumFieldInfo.GetValue(mPropertyWindowAssociations[index].Type) };

                    mSelectedType.InvokeMember(mPropertyWindowAssociations[index].MemberName,
                        GetSetterBindingFlag(mPropertyWindowAssociations[index]), null, mSelectedObject, args);
                }

                // Perform any custom behavior related to this member being changed
                mPropertyWindowAssociations[index].OnMemberChanged(callingWindow, this);
            }

        }

        private void ChangeFloatValue(Window callingWindow)
        {
            int index = IndexOfWindow(callingWindow);
            float currentValue = ((UpDown)callingWindow).CurrentValue;

            if (index != -1 && mPropertyWindowAssociations[index].CanWrite)
            {
                if (this.mSelectedType.IsValueType)
                {
#if XBOX360 || WINDOWS_PHONE || MONODROID
                    throw new NotImplementedException();
#else
                    FieldInfo fieldInfo = mSelectedType.GetField(
                        mPropertyWindowAssociations[index].MemberName);

                    fieldInfo.SetValueDirect(
                        __makeref(mSelectedObject), currentValue);
#endif
                }
                else
                {
                    object[] args = { currentValue };

                    mSelectedType.InvokeMember(mPropertyWindowAssociations[index].MemberName,
                        GetSetterBindingFlag(mPropertyWindowAssociations[index]), null, mSelectedObject, args);
                }

                // Perform any custom behavior related to this member being changed
                mPropertyWindowAssociations[index].OnMemberChanged(callingWindow, this);
            }
        }

        private void ChangeDoubleValue(Window callingWindow)
        {
            int index = IndexOfWindow(callingWindow);
            float currentValue = ((UpDown)callingWindow).CurrentValue;

            if (index != -1 && mPropertyWindowAssociations[index].CanWrite)
            {
                if (this.mSelectedType.IsValueType)
                {
#if XBOX360 || WINDOWS_PHONE || MONODROID
                    throw new NotImplementedException();
#else
                    FieldInfo fieldInfo = mSelectedType.GetField(
                        mPropertyWindowAssociations[index].MemberName);

                    fieldInfo.SetValueDirect(
                        __makeref(mSelectedObject), currentValue);
#endif
                }
                else
                {
                    object[] args = { currentValue };

                    mSelectedType.InvokeMember(mPropertyWindowAssociations[index].MemberName,
                        GetSetterBindingFlag(mPropertyWindowAssociations[index]), null, mSelectedObject, args);
                }

                // Perform any custom behavior related to this member being changed
                mPropertyWindowAssociations[index].OnMemberChanged(callingWindow, this);
            }
        }

        private void ChangeIntValue(Window callingWindow)
        {
            int index = IndexOfWindow(callingWindow);
            int currentValue = (int)(((UpDown)callingWindow).CurrentValue);

            if (index != -1 && mPropertyWindowAssociations[index].CanWrite)
            {
                if (this.mSelectedType.IsValueType)
                {
#if XBOX360 || WINDOWS_PHONE || MONODROID
                    throw new NotImplementedException();
#else
                    FieldInfo fieldInfo = mSelectedType.GetField(
                        mPropertyWindowAssociations[index].MemberName);
                    if (fieldInfo != null)
                    {
                        fieldInfo.SetValueDirect(
                            __makeref(mSelectedObject), currentValue);
                    }
                    else
                    {
                        PropertyInfo propertyInfo = mSelectedType.GetProperty(
                            mPropertyWindowAssociations[index].MemberName);

                        object boxed = mSelectedObject;
                        propertyInfo.SetValue(boxed, currentValue, null);
                        mSelectedObject = (T)boxed;
                    }
#endif

                }
                else
                {
                    object[] args = { currentValue };

                    mSelectedType.InvokeMember(mPropertyWindowAssociations[index].MemberName,
                        GetSetterBindingFlag(mPropertyWindowAssociations[index]), null, mSelectedObject, args);
                }

                // Perform any custom behavior related to this member being changed
                mPropertyWindowAssociations[index].OnMemberChanged(callingWindow, this);
            }
        }

        private void ChangeLongValue(Window callingWindow)
        {
            int index = IndexOfWindow(callingWindow);

            // Make the undo
            if (mUndoInstructions != null)
            {
                long oldValue = (long)
                    GetSelectedObjectsMember(mPropertyWindowAssociations[index].MemberName);
                InstructionList instructionList = new InstructionList();

                instructionList.Add(
                    new Instruction<T, long>(mSelectedObject,
                    mPropertyWindowAssociations[index].MemberName, oldValue, 0));

                mUndoInstructions.Add(instructionList);
            }


            long currentValue = long.Parse(((TextBox)callingWindow).Text);

            if (index != -1)
            {
                if (this.mSelectedType.IsValueType)
                {
#if XBOX360 || WINDOWS_PHONE || MONODROID
                    throw new NotImplementedException();
#else
                    FieldInfo fieldInfo = mSelectedType.GetField(
                        mPropertyWindowAssociations[index].MemberName);

                    fieldInfo.SetValueDirect(
                        __makeref(mSelectedObject), currentValue);
#endif
                }
                else
                {
                    object[] args = { currentValue };

                    mSelectedType.InvokeMember(mPropertyWindowAssociations[index].MemberName,
                        GetSetterBindingFlag(mPropertyWindowAssociations[index]), null, mSelectedObject, args);
                }
                // Perform any custom behavior related to this member being changed
                mPropertyWindowAssociations[index].OnMemberChanged(callingWindow, this);
            }
        }

        private void ChangeCharValue(Window callingWindow)
        {
            if (this.mSelectedObject == null)
                return;

            int index = IndexOfWindow(callingWindow);

            char currentValue = '\0';


            if (callingWindow is TextBox && ((TextBox)callingWindow).Text.Length > 0)
                currentValue = ((TextBox)callingWindow).Text[0];
            else if (callingWindow is ComboBox)
            {
                currentValue = (char)((ComboBox)callingWindow).SelectedObject;
                if (currentValue == '\0' && ((ComboBox)callingWindow).Text != null &&
                    ((ComboBox)callingWindow).Text.Length > 0)
                    currentValue = (callingWindow as ComboBox).Text[0];
            }
            else if (callingWindow is FileTextBox && ((FileTextBox)callingWindow).Text.Length > 0)
                currentValue = ((FileTextBox)callingWindow).Text[0];

            if (index != -1)
            {
                if (this.mSelectedType.IsValueType)
                {
#if XBOX360 || WINDOWS_PHONE || MONODROID
                    throw new NotImplementedException();
#else
                    FieldInfo fieldInfo = mSelectedType.GetField(
                        mPropertyWindowAssociations[index].MemberName);

                    fieldInfo.SetValueDirect(
                        __makeref(mSelectedObject), currentValue);
#endif
                }
                else
                {
                    object[] args = { currentValue };

                    mSelectedType.InvokeMember(mPropertyWindowAssociations[index].MemberName,
                        GetSetterBindingFlag(mPropertyWindowAssociations[index]), null, mSelectedObject, args);
                }

                // Perform any custom behavior related to this member being changed
                mPropertyWindowAssociations[index].OnMemberChanged(callingWindow, this);
            }
        }

        private void ChangeStringValue(Window callingWindow)
        {
            if (this.mSelectedObject == null)
                return;

            int index = IndexOfWindow(callingWindow);

            string currentValue = "";
            

            if (callingWindow is TextBox)
                currentValue = ((TextBox)callingWindow).Text;
            else if (callingWindow is ComboBox)
            {
                currentValue = ((ComboBox)callingWindow).SelectedObject as string;
                if (currentValue == null && ((ComboBox)callingWindow).Text != null)
                    currentValue = (callingWindow as ComboBox).Text;
            }
            else if (callingWindow is FileTextBox)
                currentValue = ((FileTextBox)callingWindow).Text;

            if (index != -1)
            {
                if (this.mSelectedType.IsValueType)
                {
#if XBOX360 || WINDOWS_PHONE || MONODROID
                    throw new NotImplementedException();
#else
                    FieldInfo fieldInfo = mSelectedType.GetField(
                        mPropertyWindowAssociations[index].MemberName);

                    fieldInfo.SetValueDirect(
                        __makeref(mSelectedObject), currentValue);
#endif
                }
                else
                {
                    object[] args = { currentValue };

                    string memberName = mPropertyWindowAssociations[index].MemberName;
                    BindingFlags bindingFlags = GetSetterBindingFlag(mPropertyWindowAssociations[index]);
                    mSelectedType.InvokeMember(memberName, bindingFlags, null, mSelectedObject, args);
                }

                // Perform any custom behavior related to this member being changed
                mPropertyWindowAssociations[index].OnMemberChanged(callingWindow, this);
            }
        }

        private void ChangeShortValue(Window callingWindow)
        {
            int index = IndexOfWindow(callingWindow);
            short currentValue = (short)(((UpDown)callingWindow).CurrentValue);

            if (index != -1)
            {
                if (this.mSelectedType.IsValueType)
                {
#if XBOX360 || WINDOWS_PHONE || MONODROID
                    throw new NotImplementedException();
#else
                    FieldInfo fieldInfo = mSelectedType.GetField(
                        mPropertyWindowAssociations[index].MemberName);

                    fieldInfo.SetValueDirect(
                        __makeref(mSelectedObject), currentValue);
#endif
                }
                else
                {
                    object[] args = { currentValue };

                    mSelectedType.InvokeMember(mPropertyWindowAssociations[index].MemberName,
                        GetSetterBindingFlag(mPropertyWindowAssociations[index]), null, mSelectedObject, args);
                }

                // Perform any custom behavior related to this member being changed
                mPropertyWindowAssociations[index].OnMemberChanged(callingWindow, this);
            }
        }

        private void ChangeTexture(Window callingWindow)
        {
            FileWindow fileWindow = GuiManager.AddFileWindow();
            fileWindow.SetFileType("graphic");

            fileWindow.SetToLoad();
            fileWindow.OkClick += ChangeTextureLoadOk;

            mCachedPropertyName =
                mPropertyWindowAssociations[IndexOfWindow(callingWindow)].MemberName;
        }

        private void ChangeVector3Value(Window callingWindow)
        {
            int index = IndexOfWindow(callingWindow);
            Vector3 currentValue = ((Vector3Display)callingWindow).Vector3Value;

            if (index != -1)
            {
                if (this.mSelectedType.IsValueType)
                {
#if XBOX360 || WINDOWS_PHONE || MONODROID
                    throw new NotImplementedException();
#else
                    FieldInfo fieldInfo = mSelectedType.GetField(
                        mPropertyWindowAssociations[index].MemberName);

                    fieldInfo.SetValueDirect(
                        __makeref(mSelectedObject), currentValue);
#endif
                }
                else
                {
                    object[] args = { currentValue };

                    mSelectedType.InvokeMember(mPropertyWindowAssociations[index].MemberName,
                        GetSetterBindingFlag(mPropertyWindowAssociations[index]), null, mSelectedObject, args);
                }

                // Perform any custom behavior related to this member being changed
                mPropertyWindowAssociations[index].OnMemberChanged(callingWindow, this);
            }
        }

        private void ChangeVector2Value(Window callingWindow)
        {
            int index = IndexOfWindow(callingWindow);
            Vector2 currentValue = ((Vector3Display)callingWindow).Vector2Value;

            if (index != -1)
            {
                if (this.mSelectedType.IsValueType)
                {
#if XBOX360 || WINDOWS_PHONE || MONODROID
                    throw new NotImplementedException();
#else
                    FieldInfo fieldInfo = mSelectedType.GetField(
                        mPropertyWindowAssociations[index].MemberName);

                    fieldInfo.SetValueDirect(
                        __makeref(mSelectedObject), currentValue);
#endif
                }
                else
                {
                    object[] args = { currentValue };

                    mSelectedType.InvokeMember(mPropertyWindowAssociations[index].MemberName,
                        GetSetterBindingFlag(mPropertyWindowAssociations[index]), null, mSelectedObject, args);
                }

                // Perform any custom behavior related to this member being changed
                mPropertyWindowAssociations[index].OnMemberChanged(callingWindow, this);
            }
        }

        private void ChangeColorValue(Window callingWindow)
        {
            int index = IndexOfWindow(callingWindow);
            Color currentValue = ((ColorDisplay)callingWindow).ColorValue;

            if (index != -1)
            {
                if (this.mSelectedType.IsValueType)
                {
#if XBOX360 || WINDOWS_PHONE || MONODROID
                    throw new NotImplementedException();
#else
                    FieldInfo fieldInfo = mSelectedType.GetField(
                        mPropertyWindowAssociations[index].MemberName);

                    fieldInfo.SetValueDirect(
                        __makeref(mSelectedObject), currentValue);
#endif
                }
                else
                {
                    object[] args = { currentValue };

                    mSelectedType.InvokeMember(mPropertyWindowAssociations[index].MemberName,
                        GetSetterBindingFlag(mPropertyWindowAssociations[index]), null, mSelectedObject, args);
                }

                // Perform any custom behavior related to this member being changed
                mPropertyWindowAssociations[index].OnMemberChanged(callingWindow, this);
            }
        }

        private void ChangeObject(Window callingWindow)
        {
            if (this.mSelectedObject == null)
                return;

            int index = IndexOfWindow(callingWindow);

            // Make the undo
            if (mUndoInstructions != null)
            {
                object oldValue =
                    GetSelectedObjectsMember(mPropertyWindowAssociations[index].MemberName);
                InstructionList instructionList = new InstructionList();

                instructionList.Add(
                    new Instruction<T, object>(mSelectedObject,
                    mPropertyWindowAssociations[index].MemberName, oldValue, 0));

                mUndoInstructions.Add(instructionList);
            }

            object currentValue = null;

            if (callingWindow is ComboBox)
                currentValue = ((ComboBox)callingWindow).SelectedObject;

            if (index != -1)
            {
                if (this.mSelectedType.IsValueType)
                {
#if XBOX360 || WINDOWS_PHONE || MONODROID
                    throw new NotImplementedException();
#else
                    FieldInfo fieldInfo = mSelectedType.GetField(
                        mPropertyWindowAssociations[index].MemberName);

                    fieldInfo.SetValueDirect(
                        __makeref(mSelectedObject), currentValue);
#endif
                }
                else
                {
                    object[] args = { currentValue };

                    mSelectedType.InvokeMember(mPropertyWindowAssociations[index].MemberName,
                        GetSetterBindingFlag(mPropertyWindowAssociations[index]), null, mSelectedObject, args);
                }

                // Perform any custom behavior related to this member being changed
                mPropertyWindowAssociations[index].OnMemberChanged(callingWindow, this);
            }
        }

        private void ChangeTextureLoadOk(Window callingWindow)
        {
            if (string.IsNullOrEmpty(mContentManagerName))
            {
                throw new InvalidOperationException("Cannot load texture because a valid ContentManagerName " +
                    "has not been set.  Set the PropertyGrid's ContentManagerName property when instantiating it.");
            }

            if (String.IsNullOrEmpty(mCachedPropertyName) == false)
            {
                string fileName = ((FileWindow)callingWindow).Results[0];
                try
                {
                    Texture2D newTexture =
                        FlatRedBallServices.Load<Texture2D>(fileName, mContentManagerName);


                    SetMemberTexture2D(mCachedPropertyName, newTexture);
                }
                catch (Exception e)
                {
                    GuiManager.ShowMessageBox("Could not load the texture\n\n" + fileName + "\n\nError Details:\n\n" +
                        e.ToString(), "Error loading texture");
                }
            }
        }

        public void EditPropertyPress(Window callingWindow)
        {
            int index = IndexOfWindow(callingWindow);

            if (index != -1)
            {

                string propertyName = mPropertyWindowAssociations[index].MemberName;

                #region If there is already ChildFloatingWindow for this property
                // The user may have clicked
                // on a property which has already
                // been clicked on before.  Check the
                // floating windows to see if there is
                // already a PropertyGrid which is displaying
                // the clicked-on property.
                if (mPropertyWindowAssociations[index].ChildFloatingWindow != null)
                {
                    mPropertyWindowAssociations[index].ChildFloatingWindow.Visible = true;
                    GuiManager.BringToFront(mPropertyWindowAssociations[index].ChildFloatingWindow);

                    // Refresh the property:
                    BindingFlags flags = GetGetterBindingFlag(mPropertyWindowAssociations[index]);
                    Binder binder = null;
                    object[] args = null;


                    object result = mSelectedType.InvokeMember(
                       mPropertyWindowAssociations[index].MemberName,
                       flags,
                       binder,
                       mSelectedObject,
                       args
                       );

                    if (mPropertyWindowAssociations[index].ChildFloatingWindow is PropertyGrid)
                    {


                        mPropertyWindowAssociations[index].SetPropertyFor(
                            mPropertyWindowAssociations[index].ChildFloatingWindow as PropertyGrid, result);
                    }
                    else if (mPropertyWindowAssociations[index].ChildFloatingWindow is ListDisplayWindow)
                    {
                        // refresh the list shown
                        (mPropertyWindowAssociations[index].ChildFloatingWindow as ListDisplayWindow).ListShowing =
                            result as IEnumerable;

                    }

                }
                #endregion

                #region Else, need to create a new window
                else
                {
                    // If we're here then it's time to create a window
                    CreateFloatingWindowForProperty(mPropertyWindowAssociations[index]);
                }
                #endregion
            }
        }

        protected override void ExtraWindowResize(Window callingWindow)
        {
            UpdateScaleAndWindowPositions();
        }

        private void NullSettingListBoxClick(Window callingWindow)
        {
            ListBox asListBox = callingWindow as ListBox;

            CollapseItem item = asListBox.GetHighlightedItem();

            if (item.Text == "Set to null")
            {
                SetMemberTexture2D(asListBox.Name, null);

            }
            GuiManager.RemoveWindow(callingWindow);
        }


        void RaiseChangeEventForFont(IWindow callingWindow)
        {
            PropertyWindowAssociation pwa = GetPropertyWindowAssociationForMember("Font");

            pwa.OnMemberChanged(callingWindow, this);
        }

        private void OpenChangeBitmapFontWindow(Window callingWindow)
        {
            BitmapFontChangePropertyGrid mBitmapFontSave = new BitmapFontChangePropertyGrid(
                mCursor, mSelectedObject, "Font");
            mBitmapFontSave.ContentManagerName = ContentManagerName;

            mBitmapFontSave.OkClick += RaiseChangeEventForFont;

            GuiManager.AddWindow(mBitmapFontSave);
        }

        private void RemoveSelfFromPwaChildFloatingWindow(Window callingWindow)
        {
            for (int i = 0; i < mPropertyWindowAssociations.Count; i++)
            {
                if (mPropertyWindowAssociations[i].ChildFloatingWindow == callingWindow)
                {
                    mPropertyWindowAssociations[i].ChildFloatingWindow = null;
                }
            }
        }

        private void ResizeEvent(Window callingWindow)
        {
            if (mWavListBox != null)
            {
                mWavListBox.ScaleY = callingWindow.ScaleY - .5f;
                mWavListBox.Y = callingWindow.ScaleY;
                mWavListBox.X = mWavListBox.ScaleX + .5f;
            }
        }

        private void StoreUndoByte(Window callingWindow)
        {
            if (mUndoInstructions != null)
            {
                string memberName = GetMemberNameForUIElement(callingWindow);

                UpDown upDown = callingWindow as UpDown;

                byte oldValue = (byte)upDown.BeforeChangeValue;

                if (oldValue != (byte)upDown.UnroundedCurrentValue)
                {

                    InstructionList instructionList = new InstructionList();

                    instructionList.Add(
                        new Instruction<T, byte>(mSelectedObject, memberName, oldValue, 0));

                    mUndoInstructions.Add(instructionList);
                }
            }
        }

        private void StoreUndoChar(Window callingWindow)
        {
            int index = IndexOfWindow(callingWindow);

            // Make the undo
            if (mUndoInstructions != null)
            {
                char oldValue = (char)
                    GetSelectedObjectsMember(mPropertyWindowAssociations[index].MemberName);
                InstructionList instructionList = new InstructionList();

                instructionList.Add(
                    new Instruction<T, char>(mSelectedObject,
                    mPropertyWindowAssociations[index].MemberName, oldValue, 0));

                mUndoInstructions.Add(instructionList);
            }

        }

        private void StoreUndoString(Window callingWindow)
        {
            int index = IndexOfWindow(callingWindow);

            // Make the undo
            if (mUndoInstructions != null)
            {
                string oldValue = (string)
                    GetSelectedObjectsMember(mPropertyWindowAssociations[index].MemberName);
                InstructionList instructionList = new InstructionList();

                instructionList.Add(
                    new Instruction<T, string>(mSelectedObject,
                    mPropertyWindowAssociations[index].MemberName, oldValue, 0));

                mUndoInstructions.Add(instructionList);
            }
        }

        private void StoreUndoFloat(Window callingWindow)
        {
            if (mUndoInstructions != null)
            {
                string memberName = GetMemberNameForUIElement(callingWindow);

                UpDown upDown = callingWindow as UpDown;

                float oldValue = upDown.BeforeChangeValue;
                if (oldValue != upDown.UnroundedCurrentValue)
                {
                    InstructionList instructionList = new InstructionList();

                    instructionList.Add(
                        new Instruction<T, float>(mSelectedObject, memberName, oldValue, 0));

                    mUndoInstructions.Add(instructionList);
                }
            }
        }

        private void StoreUndoDouble(Window callingWindow)
        {
            if (mUndoInstructions != null)
            {
                string memberName = GetMemberNameForUIElement(callingWindow);

                UpDown upDown = callingWindow as UpDown;

                if (upDown.BeforeChangeValue != upDown.CurrentValue)
                {
                    double oldValue = (double)upDown.BeforeChangeValue;

                    InstructionList instructionList = new InstructionList();

                    instructionList.Add(
                        new Instruction<T, double>(mSelectedObject, memberName, oldValue, 0));

                    mUndoInstructions.Add(instructionList);
                }
            }
        }

        private void StoreUndoInt(Window callingWindow)
        {
            if (mUndoInstructions != null)
            {
                string memberName = GetMemberNameForUIElement(callingWindow);

                UpDown upDown = callingWindow as UpDown;

                int oldValue = (int)upDown.BeforeChangeValue;

                if (oldValue != (int)upDown.UnroundedCurrentValue)
                {

                    InstructionList instructionList = new InstructionList();

                    instructionList.Add(
                        new Instruction<T, int>(mSelectedObject, memberName, oldValue, 0));

                    mUndoInstructions.Add(instructionList);
                }
            }
        }

        private void StoreUndoShort(Window callingWindow)
        {
            if (mUndoInstructions != null)
            {
                string memberName = GetMemberNameForUIElement(callingWindow);

                UpDown upDown = callingWindow as UpDown;

                short oldValue = (short)upDown.BeforeChangeValue;

                if (oldValue != (short)upDown.UnroundedCurrentValue)
                {

                    InstructionList instructionList = new InstructionList();

                    instructionList.Add(
                        new Instruction<T, short>(mSelectedObject, memberName, oldValue, 0));

                    mUndoInstructions.Add(instructionList);
                }
            }
        }

        private void StoreUndoVector2(Window callingWindow)
        {
            if (mUndoInstructions != null)
            {
                string memberName = GetMemberNameForUIElement(callingWindow);

                Vector3Display vector3Display = callingWindow as Vector3Display;

                Vector2 oldValue = vector3Display.BeforeChangeVector2Value;
                if (oldValue != vector3Display.Vector2Value)
                {
                    InstructionList instructionList = new InstructionList();

                    instructionList.Add(
                        new Instruction<T, Vector2>(mSelectedObject, memberName, oldValue, 0));

                    mUndoInstructions.Add(instructionList);
                }
            }
        }

        private void StoreUndoVector3(Window callingWindow)
        {
            if (mUndoInstructions != null)
            {
                string memberName = GetMemberNameForUIElement(callingWindow);

                Vector3Display vector3Display = callingWindow as Vector3Display;

                Vector3 oldValue = vector3Display.BeforeChangeVector3Value;
                if (oldValue != vector3Display.Vector3Value)
                {
                    InstructionList instructionList = new InstructionList();

                    instructionList.Add(
                        new Instruction<T, Vector3>(mSelectedObject, memberName, oldValue, 0));

                    mUndoInstructions.Add(instructionList);
                }
            }
        }

        private void StoreUndoColor(Window callingWindow)
        {
            if (mUndoInstructions != null)
            {
                string memberName = GetMemberNameForUIElement(callingWindow);

                ColorDisplay colorDisplay = callingWindow as ColorDisplay;

                Color oldValue = colorDisplay.BeforeChangeColorValue;

                if (oldValue != colorDisplay.ColorValue)
                {
                    InstructionList instructionList = new InstructionList();

                    instructionList.Add(
                        new Instruction<T, Color>(mSelectedObject, memberName, oldValue, 0));

                    mUndoInstructions.Add(instructionList);
                }
            }
        }

        private void ShowRightClickMenu(Window callingWindow)
        {
            ListBox listBox = GuiManager.AddPerishableListBox();
            listBox.AddItem("Set to null");
            listBox.Click += NullSettingListBoxClick;
            listBox.HighlightOnRollOver = true;

            listBox.ScrollBarVisible = false;
            listBox.SetScaleToContents(4);


            string propertyName = GetMemberNameForUIElement(callingWindow);

            listBox.Name = propertyName;


            GuiManager.PositionTopLeftToCursor(listBox);
        }

        #endregion

        #region Methods

        #region Constructor

        public PropertyGrid(Cursor cursor)
            : base(cursor)
        {


			this.Closing += CloseWindow;

            mSelectedType = typeof(T);
            // Set the name (and visible title) of the window if one hasn't 
            // already been set.
            Name = mSelectedType.ToString();

            mPropertyWindowAssociations = new List<PropertyWindowAssociation>();

            ScaleX = 12;

            HasMoveBar = true;

            Resizing += ResizeEvent;

            MinimumScaleY = 7;

            mExcludedMembers = new List<string>();

            CreateWindowsForSelectedType();

            SelectedObject = default(T);
			this.SetPositionTL(LastXPosition, LastYPosition);
		}

		private void CloseWindow(Window callingWindow)
		{
			LastXPosition = X;
			LastYPosition = Y;
		}

        #endregion

        #region Public Methods

        #region Exclude Member Methods
        public override void ExcludeAllMembers()
        {
            for (int i = mPropertyWindowAssociations.Count - 1; 
                i > -1; i--)
            {
                if (!string.IsNullOrEmpty(mPropertyWindowAssociations[i].MemberName) &&
                    mPropertyWindowAssociations[i].Window != null)
                {
                    RemoveWindowBase(mPropertyWindowAssociations[i].Window);
                    RemoveWindowBase(mPropertyWindowAssociations[i].Label);
                    mPropertyWindowAssociations[i].ClearChangeEvent();
                    mPropertyWindowAssociations.RemoveAt(i);
                }
            }

            PropertyInfo[] properties = mSelectedType.GetProperties();
            foreach (PropertyInfo property in properties)
            {
                mExcludedMembers.Add(property.Name);
            }

            FieldInfo[] fields = mSelectedType.GetFields();
            foreach (FieldInfo field in fields)
            {
                mExcludedMembers.Add(field.Name);
            }

            UpdateScaleAndWindowPositions();
            UpdateToObject();
        }


        public void ExcludeEnumerationValue(string member, string value)
        {
            PropertyWindowAssociation pwa =
                GetPropertyWindowAssociationForMember(member);

            if (pwa == null)
            {
                throw new ArgumentException("There is no member by the name " + member +
                    " included in this PropertyGrid");
            }

            Type type = pwa.Type;

            List<string> excludedValues = null;

            if (mExcludedEnumerationValues.ContainsKey(type))
            {
                excludedValues = mExcludedEnumerationValues[type];
            }
            else
            {
                excludedValues = new List<string>();
                mExcludedEnumerationValues.Add(type, excludedValues);
            }

            excludedValues.Add(value);

            SetEnumerationValuesForComboBox(pwa);
        }


        public override void ExcludeMember(string propertyToExclude)
        {
            // remove the property from the mPropertyWindowAssociations and the window associated with it

            PropertyWindowAssociation pwa = GetPropertyWindowAssociationForMember(propertyToExclude);

            if (pwa != null)
            {
                if (pwa.Window != null)
                {
                    RemoveWindowBase(pwa.Window);
                    RemoveWindowBase(pwa.Label);

                    pwa.ClearChangeEvent();
                }
                mPropertyWindowAssociations.Remove(pwa);

                UpdateScaleAndWindowPositions();
            }
        }


        public void ExcludeMembersInType<E>()
        {
            ExcludeMembersInType(typeof(E));
        }


        public override void ExcludeMembersInType(Type typeContainingMembersToExclude)
        {
            PropertyInfo[] properties = typeContainingMembersToExclude.GetProperties();

            foreach (PropertyInfo propertyInfo in properties)
            {
                ExcludeMember(propertyInfo.Name);
            }

            FieldInfo[] fields = typeContainingMembersToExclude.GetFields();

            foreach (FieldInfo fieldInfo in fields)
            {
                ExcludeMember(fieldInfo.Name);
            }

        }


        public override void ExcludeStaticMembers()
        {
            PropertyInfo[] properties =
                typeof(T).GetProperties(BindingFlags.Static | BindingFlags.Public);

            foreach (PropertyInfo propertyInfo in properties)
            {
                ExcludeMember(propertyInfo.Name);
            }

            FieldInfo[] fields = typeof(T).GetFields(BindingFlags.Static | BindingFlags.Public);

            foreach (FieldInfo fieldInfo in fields)
            {
                ExcludeMember(fieldInfo.Name);
            }

        }

        #endregion

        #region XML Docs
        /// <summary>
        /// Gets the window that is used for editing the argument memberName.
        /// </summary>
        /// <remarks>
        /// For example, if the member is a bool then this method will return a 
        /// ComboBox.  If the member is of a type that does not have a specific UI
        /// representation then a button that says "Edit Property" will be returned.
        /// </remarks>
        /// <param name="propertyName">The name of the member.</param>
        /// <returns>The UI element that is used to edit this property, or null if the property is not found.</returns>
        #endregion
        public override IWindow GetUIElementForMember(string memberName)
        {
            PropertyWindowAssociation pwa = GetPropertyWindowAssociationForMember(memberName);
            if (pwa != null)
            {
                return pwa.Window;
            }
            else
            {
                return null;
            }
        }


        public Window GetFloatingChildUIElementForMember(string memberName)
        {
            PropertyWindowAssociation pwa = GetPropertyWindowAssociationForMember(memberName);

            if (pwa != null)
            {
                if (pwa.ChildFloatingWindow == null)
                {
                    CreateFloatingWindowForProperty(pwa);
                }

                return pwa.ChildFloatingWindow;
            }
            else
            {
                return null;

            }
        }


        public override string GetMemberNameForUIElement(Window window)
        {
            int index = IndexOfWindow(window);
            if (index != -1)
            {
                return mPropertyWindowAssociations[index].MemberName;
            }
            else
            {
                return "";
            }
        }


        public override IWindow IncludeMember(string propertyToInclude)
        {
            // If there is already a WAVListBox, we want to use the overload that will categorize this:
            if (mWavListBox != null)
            {
                return IncludeMember(propertyToInclude, mUncategorizedCategoryName);
            }
            else
            {
                IWindow window = null;

                if (mExcludedMembers.Contains(propertyToInclude))
                {
                    mExcludedMembers.Remove(propertyToInclude);
                }

                // make sure this property is not already included
                if (GetPropertyWindowAssociationForMember(propertyToInclude) == null)
                {
                    PropertyWindowAssociation pwa = CreatePropertyWindowAssociation(propertyToInclude);

                    if (pwa == null)
                    {
                        throw new ArgumentException("Cannot find field or property " + propertyToInclude);

                    }

                    mPropertyWindowAssociations.Add(pwa);

                    CreateWindow(pwa, true);

                    window = pwa.Window;

                    // Vic says - these used to be outside of the
                    // if statement.  In other words, they used to 
                    // be called whether a new Window was created or
                    // not.  But it turns out that caused infinite recursion
                    // and potential performance problems.  This shouldn't hurt
                    // behavior and should fix both problems.
                    UpdateScaleAndWindowPositions();

                    UpdateUIAndDisplayedProperties();
                }
                else
                {
                    window = GetUIElementForMember(propertyToInclude);
                }
                return window;
            }
        }


        public override IWindow IncludeMember(string propertyToInclude, string category)
        {
            return IncludeMember(propertyToInclude, category, true);
        }

        public IWindow IncludeMember(string propertyToInclude, string category, bool callUpdateDisplayedProperties)
        {
            if (mExcludedMembers.Contains(propertyToInclude))
            {
                mExcludedMembers.Remove(propertyToInclude);
            }


            #region Check if the WindowArrayVisibilityListBox has already been created

            CreateWavListBoxIfNecessary();

            #endregion

            string selectedCategory = mUncategorizedCategoryName;
            // Is this needed: ?
            if (mWavListBox.GetFirstHighlightedItem() != null)
            {
                selectedCategory = mWavListBox.GetFirstHighlightedItem().Text;
            }

            #region Check if the WindowArrayVisibilityListBox has the given category
            if (mWavListBox.Contains(category) == false)
            {
                mWavListBox.AddWindowArray(category, new WindowArray());

            }
            #endregion

            PropertyWindowAssociation pwa = GetPropertyWindowAssociationForMember(propertyToInclude);

            if (pwa == null)
            {

                pwa = CreatePropertyWindowAssociation(propertyToInclude);

                if (pwa == null)
                {
                    throw new ArgumentException("Cannot find field or property " + propertyToInclude);

                }
                mPropertyWindowAssociations.Add(pwa);

                CreateWindow(pwa, callUpdateDisplayedProperties);
            }


            pwa.Category = category;

            UpdateWavMembership();

            UpdateScaleAndWindowPositions();

            UpdateUIAndDisplayedProperties();

            SelectCategory(selectedCategory);

            return GetUIElementForMember(propertyToInclude);
        }


        public void RaiseMemberChangeEvent(string memberName)
        {
            PropertyWindowAssociation pwa = GetPropertyWindowAssociationForMember(memberName);

            // Perform any custom behavior related to this member being changed
            pwa.OnMemberChanged(pwa.Window, this);

        }


        public void RemoveCategoryAndContainingMembers(string categoryToRemove)
        {
            for (int i = 0; i < mPropertyWindowAssociations.Count; i++)
            {
                if (mPropertyWindowAssociations[i].Window != null)
                    RemoveWindow(mPropertyWindowAssociations[i].Window);

                mPropertyWindowAssociations.RemoveAt(i);
            }

            RemoveCategory(categoryToRemove);
        }

        #region XML Docs
        /// <summary>
        /// Replaces the UI element representing the argument propertyName with the argument newUIElement.
        /// </summary>
        /// <remarks>
        /// The newUIElement argument should not already be added to the GuiManager or to the PropertyGrid.  This method
        /// should be called BEFORE any UI events are added to the newUIElement argument.  This guarantees that
        /// the PropertyGrid events are fired first, then the custom events can be used to modify the object shown
        /// by the PropertyGrid.
        /// </remarks>
        /// <param name="propertyName">The name of the property to replace the UI element for.</param>
        /// <param name="newUIElement">The new UI element for the property.  This should be created with a new call, 
        /// not added to this instance or the GuiManager.</param>
        #endregion
        public override void ReplaceMemberUIElement(string propertyName, IWindow newUIElement)
        {
            PropertyWindowAssociation pwa = GetPropertyWindowAssociationForMember(propertyName);

            if (pwa != null)
            {
                IWindow windowAtIndex = pwa.Window;

                string oldCategory = "";
                bool wasEnabled = windowAtIndex.Enabled;

                if (mWavListBox != null)
                {
                    oldCategory = mWavListBox.GetCategoryWindowBelongsTo(windowAtIndex);
                    mWavListBox.RemoveWindow(windowAtIndex);
                }

                // Delete the old UI element.
                // This needs to happen AFTER we get the old category, or else
                // the indexes will be all wacky
                RemoveWindowBase(windowAtIndex);
                                
                // Add the new UI element.
                AddWindowBase(newUIElement);
                pwa.Window = newUIElement;
                if (mWavListBox != null && oldCategory != "")
                {
                    mWavListBox.AddWindowToCategory(newUIElement, oldCategory);
                }

                newUIElement.Enabled = wasEnabled;
                ApplyDefaultBehavior(pwa);

                // Reposition the UI elements.
                UpdateScaleAndWindowPositions();

                // Set the new UI element's value
                UpdateDisplayedProperties();
                //}
            }
        }


        public override void SetConditionalMemberVisibility<MemberType>(
            string memberToTest,
            Operator operatorToUse,
            MemberType value,
            VisibilitySetting visibilitySetting,
            string memberToIncludeOrExclude)
        {
            PropertyWindowAssociation pwa = GetPropertyWindowAssociationForMember(memberToTest);

            pwa.mMemberVisibleConditions.Add(
                new MemberVisibleCondition(
                    new MemberCondition<T, MemberType>(default(T), memberToTest, value, operatorToUse),
                    visibilitySetting,
                    memberToIncludeOrExclude));

        }


        public override void SetMemberDisplayName(string member, string displayName)
        {
            PropertyWindowAssociation pwa = GetPropertyWindowAssociationForMember(member);

            if (pwa != null)
            {
                pwa.Label.Text = displayName;

                // Since this may require making the window larger/smaller, update the scales and positions.
                UpdateScaleAndWindowPositions();
            }
        }




        public override void SetOptionsForMember(string member, IList<string> availableOptions)
        {
            SetOptionsForMember(member, availableOptions, true);
        }


        public override void SetOptionsForMember(string member, IList<string> availableOptions, bool forceOptions)
        {
            if (forceOptions)
            {
                ComboBox comboBox = GetUIElementForMember(member) as ComboBox;
                if (comboBox == null)
                {
                    comboBox = new ComboBox(mCursor);
                    comboBox.ScaleX = 7;
                    ReplaceMemberUIElement(member, comboBox);
                }
                else
                {
                    comboBox.Clear();
                }

                // It's acceptable for the user to pass null as available options.  This will create a combo box
                // which has no available options. These can then be set later.
                if (availableOptions != null)
                {
                    foreach (string option in availableOptions)
                    {
                        comboBox.AddItem(option);
                    }
                }
            }
            else
            {
                TextBox memberTextBox = GetUIElementForMember(member) as TextBox;

                if (memberTextBox == null)
                {
                    throw new ArgumentException("The member " + member + " does not use a text box as its " +
                        "displaying UI element.  This is a requirement if options are not forced.");
                }

                memberTextBox.SetOptions(availableOptions);

            }


        }

        public override void SetOptionsForMember(string member, IDictionary<string, object> dict) 
        {
            ComboBox comboBox = new ComboBox(mCursor);
            comboBox.ScaleX = 7;
            foreach (string key in dict.Keys)
            {
                comboBox.AddItem(key, dict[key]);
            }
            ReplaceMemberUIElement(member, comboBox);
        }

        public override void SetOptionsForMember(string member, ICollection collection, StringRepresentation rep)
        {
            ComboBox comboBox = GetUIElementForMember(member) as ComboBox;
            if (comboBox == null)
            {
                comboBox = new ComboBox(mCursor);
                comboBox.ScaleX = 7;
                ReplaceMemberUIElement(member, comboBox);
            }
            else
            {
                comboBox.Clear();
            }

            if (collection != null)
            {
                foreach (object obj in collection)
                {
                    comboBox.AddItem(rep(obj), obj);
                }
            }


            Type typeOfMember = GetPropertyWindowAssociationForMember(member).Type;
            // If this key is already contained, then this will refresh it.
            if (ObjectDisplayManager.sStringRepresentations.ContainsKey(typeOfMember))
            {
                ObjectDisplayManager.sStringRepresentations.Remove(typeOfMember);
            }
            ObjectDisplayManager.sStringRepresentations.Add(typeOfMember, rep);
        }

        public override void UpdateDisplayedProperties()
        {
            if (mSelectedObject == null)
                return;

            PropertyWindowAssociation pwa = null;

            for (int index = 0; index < mPropertyWindowAssociations.Count; index++)
            {
                pwa = mPropertyWindowAssociations[index];

                #region Updated floating ListDisplayWindows and floating PropertyGrids

                if (pwa.ChildFloatingWindow != null)
                {
                    if (pwa.ChildFloatingWindow is ListDisplayWindow)
                    {
#if WINDOWS_PHONE || MONODROID
                        throw new NotImplementedException();
#else
                        ((ListDisplayWindow)pwa.ChildFloatingWindow).UpdateToList();
#endif
                    }
                    else if (pwa.ChildFloatingWindow is PropertyGrid)
                    {
                        ((PropertyGrid)pwa.ChildFloatingWindow).UpdateDisplayedProperties();
                    }
                }
                #endregion

                #region Update the displayed properties on this
                if (!string.IsNullOrEmpty(pwa.MemberName) &&
                    pwa.Window != null &&
                    pwa.Window.Visible &&
                    IsUserEditingWindow(pwa.Window) == false)
                {
                    UpdatePropertyWindowAssociationDisplayedProperties(pwa);
                }
                #endregion


                // Update the tab order
                UpdateTabOrder();
            }

            CallAfterUpdateDisplayedProperties();

        }

        private void UpdatePropertyWindowAssociationDisplayedProperties(PropertyWindowAssociation pwa)
        {
            // If there is a window to update and it's not being edited by the user,
            // update its value.


            //return;
            // VVV

            string propertyName = pwa.MemberName;

            object result = null;

            if (pwa.CanRead)
            {
                result = GetSelectedObjectsMember(propertyName, pwa);
            }

            // ^^^^^
            //return;

            Type type = pwa.Window.GetType();

            #region If window is UpDown
            if (type == typeof(UpDown))
            {
                if (result is float)
                    ((UpDown)pwa.Window).CurrentValue = (float)result;
                else if (result is double)
                    ((UpDown)pwa.Window).CurrentValue = (float)((double)result);
                else if (result is int)
                    ((UpDown)pwa.Window).CurrentValue = (int)result;
                else if (result is short)
                    ((UpDown)pwa.Window).CurrentValue = (short)result;
                else if (result is byte)
                    ((UpDown)pwa.Window).CurrentValue = (byte)result;
            }
            #endregion

            #region If window is TextBox

            else if (type == typeof(TextBox))
            {
                if (result == null)
                    ((TextBox)pwa.Window).Text = "";
                else if (result is char)
                    ((TextBox)pwa.Window).Text = "" + result;
                else if (result is string)
                    ((TextBox)pwa.Window).Text = (string)result;
                else if (result is long)
                    ((TextBox)pwa.Window).Text = ((long)result).ToString();
            }

            #endregion

                // ^^^^^^^
                //return;

            #region If window is FileTextBox

            else if (type == typeof(FileTextBox))
            {
                ((FileTextBox)pwa.Window).Text = (string)result;
            }

            #endregion

            #region If window is Button

            else if (type == typeof(Button))
            {

                if (result is Texture2D)
                {
                    ((Button)pwa.Window).SetOverlayTextures((Texture2D)result, null);
                    if (result != null)
                    {
                        ((Button)pwa.Window).Text = ((Texture2D)result).Name;
                    }
                }
            }

            #endregion

            #region If window is ComboBox

            else if (type == typeof(ComboBox))
            {
                if (result == null)
                {
                    ((ComboBox)pwa.Window).Text = "<No Object>";

                }
                else
                {
                    if (!(result is string))
                    {
                        ((ComboBox)pwa.Window).SelectedObject = result;
                    }

                    if (result != null &&
                        ObjectDisplayManager.sStringRepresentations.ContainsKey(result.GetType()))
                    {
                        ((ComboBox)pwa.Window).Text =
                            ObjectDisplayManager.sStringRepresentations[result.GetType()](result);
                    }
                    else if (ObjectDisplayManager.sStringRepresentations.ContainsKey(pwa.Type))
                    {
                        ((ComboBox)pwa.Window).Text =
                            ObjectDisplayManager.sStringRepresentations[pwa.Type](result);
                    }
                    else
                    {
                        ((ComboBox)pwa.Window).Text = result.ToString();
                    }
                }

            }

            #endregion

                // ^^^^^^
                //return;

            #region If window is ListDisplayWindow

            else if (type == typeof(ListDisplayWindow) || type.IsSubclassOf(typeof(ListDisplayWindow)))
            {
                ((ListDisplayWindow)pwa.Window).ListShowing = ((IEnumerable)result);
            }

            #endregion

            #region If window is Vector3Display

            else if (type == typeof(Vector3Display))
            {
                if (result is Vector2)
                {
                    ((Vector3Display)pwa.Window).Vector2Value = ((Vector2)result);
                }
                else
                {
                    ((Vector3Display)pwa.Window).Vector3Value = ((Vector3)result);
                }
            }

            #endregion

            #region If window is ColorDisplay

            else if (type == typeof(ColorDisplay))
            {
                ((ColorDisplay)pwa.Window).ColorValue = ((Color)result);
            }

            #endregion

            #region If window is PropertyGrid

            else if (type.IsSubclassOf(typeof(PropertyGrid))) // takes care of BitmapFont
            {
                ((PropertyGrid)pwa.Window).SetSelectedObject(result);
            }

            #endregion

            else if (pwa.Window is IObjectDisplayer)
            {
                bool shouldUpdate = true;

                Window asWindow = pwa.Window as Window;

                if (asWindow != null && asWindow.IsWindowOrChildrenReceivingInput)
                {
                    shouldUpdate = false;
                }

                // July 10, 2011
                // This if statement
                // wasn't here before, 
                // but it seems like it
                // should be, so I'm going
                // to put it in.
                if (shouldUpdate)
                {
                    ((IObjectDisplayer)pwa.Window).ObjectDisplayingAsObject = result;
                }
            }


        }

        #endregion

        #region Internal Methods

        internal override void SetSelectedObject(object objectToSet)
        {
            SelectedObject = (T)(objectToSet);
        }

        internal override object GetSelectedObject()
        {
            return mSelectedObject;
        }


        internal BindingFlags GetSetterBindingFlag(PropertyWindowAssociation pwa)
        {
            switch (pwa.ReferencedMemberType)
            {
                case MemberTypes.Field:
                    return BindingFlags.SetField;
                case MemberTypes.Property:
                    return BindingFlags.SetProperty;
                default:
                    return BindingFlags.SetProperty;
            }
        }


        internal BindingFlags GetSetterBindingFlag(string memberName)
        {
            PropertyWindowAssociation pwa = GetPropertyWindowAssociationForMember(memberName);

            return GetSetterBindingFlag(pwa);
        }



        #endregion

        #region Protected

        #region XML Docs
        /// <summary>
        /// Creates all of the windows (Buttons, TextBoxes, etc) for the selected type.  This is only
        /// called once when the PropertyGrid is first created.
        /// </summary>
        #endregion
        protected virtual void CreateWindowsForSelectedType()
        {
            PropertyInfo[] propertyInfos = mSelectedType.GetProperties();

            PropertyInfo[] staticPropertyInfos = mSelectedType.GetProperties(BindingFlags.Static | BindingFlags.Public);


            foreach (PropertyInfo pi in propertyInfos)
            {
                // Not sure why isStatic is here...maybe we intend to use it below?
                //bool isStatic = false;
                //for (int i = 0; i < staticPropertyInfos.Length; i++)
                //{
                //    if (staticPropertyInfos[i] == pi)
                //    {
                //        isStatic = true;
                //    }
                //}

                if (mExcludedMembers.Contains(pi.Name) == false)
                {
                    try
                    {
                        mPropertyWindowAssociations.Add(CreatePropertyWindowAssociation(pi.Name));
                    }
                    catch
                    {
                        ExcludeMember(pi.Name);
                    }
                }
            }

            FieldInfo[] fieldInfos = mSelectedType.GetFields();
            FieldInfo[] staticFieldInfos = mSelectedType.GetFields(BindingFlags.Static | BindingFlags.Public);

            foreach (FieldInfo fi in fieldInfos)
            {
                if (fi.IsPublic)
                {
                    // Not sure what this is for - maybe we intend to use it somewhere below?
                    //bool isStatic = false;
                    //for (int i = 0; i < staticFieldInfos.Length; i++)
                    //{
                    //    if (staticFieldInfos[i] == fi)
                    //    {
                    //        isStatic = true;
                    //    }
                    //}


                    if (mExcludedMembers.Contains(fi.Name) == false)
                    {
                        mPropertyWindowAssociations.Add(
                            CreatePropertyWindowAssociation(fi.Name));
                    }
                }
            }

            foreach (PropertyWindowAssociation pwa in mPropertyWindowAssociations)
            {
                // The second argument keeps things running a little faster 
                CreateWindow(pwa, false);
            }

            UpdateDisplayedProperties();

            UpdateScaleAndWindowPositions();
        }

        protected override object GetSelectedObjectsMember(string memberName, PropertyWindowAssociation pwa)
        {
            if (pwa.IsStatic)
            {
                // Update LateBinder to handle this
                BindingFlags flags = GetGetterBindingFlag(pwa);

                Binder binder = null;
                object[] args = null;

                if (pwa.CanRead == false)
                {
                    return null;
                }


                try
                {
                    object result = mSelectedType.InvokeMember(
                       memberName,
                       flags,
                       binder,
                       mSelectedObject,
                       args
                       );

                    return result;
                }
                catch
                {
                    // If this fails then
                    // the code tried to instantiate
                    // an object that isn't available
                    // to instantiate.  Like an indexer.
                    // In that case, return null;
                    return null;
                }
            }
            if (pwa.ReferencedMemberType == MemberTypes.Property)
            {
                if (mSelectedObject == null)
                    return null;

                return FlatRedBall.Instructions.Reflection.LateBinder<T>.Instance.GetProperty(
                    mSelectedObject,
                    memberName);
            }
            else
            {
                try
                {
                    return FlatRedBall.Instructions.Reflection.LateBinder<T>.Instance.GetField(
                        mSelectedObject,
                        memberName);
                }
                catch
                {
                    // If this failed it's probably a null instance, so return null:
                    return null;
                }
            }
        }

        internal override void SetSelectedObjectsMember(string memberName, object value)
        {
            PropertyWindowAssociation pwa = GetPropertyWindowAssociationForMember(memberName);

            object[] args = { value };

            if (pwa != null)
            {
                mSelectedType.InvokeMember(memberName,
                    GetSetterBindingFlag(pwa), null, mSelectedObject, args);
                // Perform any custom behavior related to this member being changed
                pwa.OnMemberChanged(
                    pwa.Window, this);
            }
        }

        internal override void SetSelectedObjectsObjectAtIndex(int index, object value)
        {
            PropertyWindowAssociation pwa = GetPropertyWindowAssociationForMember("Item");

            object[] args = { index, value };

            if (pwa != null)
            {
                mSelectedType.InvokeMember("Item",
                    GetSetterBindingFlag(pwa), null, mSelectedObject, args);

                pwa.OnMemberChanged(
                    pwa.Window, this);
            }
        }


        #endregion

        #region Private Methods

        /// <summary>
        /// Sets the event that occurs when the user changes the UI element.
        /// </summary>
        /// <param name="pwa"></param>
        private void ApplyDefaultBehavior(PropertyWindowAssociation pwa)
        {
            #region ComboBox

            if (pwa.Window is ComboBox)
            {
                if (pwa.Type == typeof(string))
                {
                    ((ComboBox)pwa.Window).ItemClick += ChangeStringValue;
                }

                else if (pwa.Type.IsSubclassOf(typeof(object)))
                {
                    ((ComboBox)pwa.Window).ItemClick += ChangeObject;
                }
            }
            #endregion

            #region FileTextBox

            else if (pwa.Window is FileTextBox)
            {
                if (pwa.Type == typeof(string))
                {
                    ((FileTextBox)pwa.Window).FileSelect += ChangeStringValue;
                    ((FileTextBox)pwa.Window).LosingFocus += ChangeStringValue;
                }
            }

            #endregion

            #region TextBox

            else if (pwa.Window is TextBox)
            {

                ((TextBox)pwa.Window).LosingFocus += ChangeStringValue;
            }

            #endregion

            #region ListDisplayWindow

            else if (pwa.Window is ListDisplayWindow)
            {
                string propertyName = pwa.MemberName;

                if (SelectedObject != null)
                {
                    object result = GetSelectedObjectsMember(propertyName);
                    if (result != null)
                    {
                        ((ListDisplayWindow)pwa.Window).ListShowing =
                            ((IEnumerable)result);
                    }
                }
            }

            #endregion

        }


        private PropertyWindowAssociation CreatePropertyWindowAssociation(string propertyName)
        {
            PropertyWindowAssociation pwa = null;

            PropertyInfo propertyInfo = mSelectedType.GetProperty(propertyName);
            PropertyInfo asStatic = mSelectedType.GetProperty(propertyName, BindingFlags.Static | BindingFlags.Public);

            if (propertyInfo != null)
            {
                ParameterInfo[] parameterInfo = propertyInfo.GetIndexParameters();

                if (parameterInfo.Length > 0)
                {
                    throw new ArgumentException("Cannot create PropertyWindowAssociation for a property that expects an argument");
                }


                bool canWrite = (propertyInfo.GetSetMethod() != null && ((propertyInfo.GetSetMethod().Attributes & MethodAttributes.Public) == MethodAttributes.Public) &&  propertyInfo.CanWrite) || DoesTypeOpenNewPropertyGrid(propertyInfo.PropertyType);

                object[] arguments = new object[]{
                        null, null,
                        propertyInfo.PropertyType,
                        propertyName,
                        propertyInfo.CanRead,
                        canWrite,
                        MemberTypes.Property,
                        asStatic != null                
                };

#if XBOX360 || WINDOWS_PHONE || MONODROID
                throw new NotImplementedException();
#else
                Type t = typeof(PropertyWindowAssociation<>).MakeGenericType(
                    propertyInfo.PropertyType);
                object obj = Activator.CreateInstance(t, arguments);
                pwa = obj as PropertyWindowAssociation;
#endif
            }
            else
            {
                FieldInfo fieldInfo = mSelectedType.GetField(propertyName);

                FieldInfo fieldInfoAsStatic = mSelectedType.GetField(propertyName, BindingFlags.Static | BindingFlags.Public);

                if (fieldInfo != null)
                {

                    object[] arguments = new object[]{
                        null, null,
                        fieldInfo.FieldType,
                        propertyName,
                        true, true,
                        MemberTypes.Field,
                        fieldInfoAsStatic != null
                    };

#if XBOX360 || WINDOWS_PHONE || MONODROID
                    throw new NotImplementedException();
#else
                    Type t = typeof(PropertyWindowAssociation<>).MakeGenericType(
                        fieldInfo.FieldType);
                    object obj = Activator.CreateInstance(t, arguments);
                    pwa = obj as PropertyWindowAssociation;
#endif
                }
            }

            return pwa;
        }


        private void CreateFloatingWindowForProperty(PropertyWindowAssociation pwa)
        {
            #region If the object selected is an IEnumerable, then create a ListBox
            if (IsIEnumerable(pwa.Type))
            {
                #region Get the instance to create the ListDisplayWindow for.

                BindingFlags flags = GetGetterBindingFlag(pwa);
                Binder binder = null;
                object[] args = null;

                IEnumerable result = mSelectedType.InvokeMember(
                   pwa.MemberName,
                   flags,
                   binder,
                   mSelectedObject,
                   args
                   ) as IEnumerable;
                #endregion

                if (result == null)
                {
                    GuiManager.ShowMessageBox("Could not create ListDisplayWindow because member " + pwa.MemberName + " is null", "null member");
                    return;
                }

                ListDisplayWindow listDisplayWindow =
                    GuiManager.ObjectDisplayManager.GetObjectDisplayerForObject(result, this) as ListDisplayWindow;


                listDisplayWindow.Name = pwa.MemberName;
                listDisplayWindow.MemberDisplaying = pwa.MemberName;


            }
            #endregion

            #region Else, the object will be shown in a PropertyGrid
            else
            {
                BindingFlags flags = GetGetterBindingFlag(pwa);
                Binder binder = null;
                object[] args = null;

                object result = null;

                try
                {

                    result = mSelectedType.InvokeMember(
                        pwa.MemberName,
                        flags,
                        binder,
                        mSelectedObject,
                        args
                        );
                }
                catch
                {
                    result = null; // object failed, show a popup
                    GuiManager.ShowMessageBox("Could not create PropertyGrid for Member " + pwa.MemberName, "Error");
                    return;
                }

                if (result == null)
                {
                    GuiManager.ShowMessageBox("Could not create PropertyGrid because member " + pwa.MemberName + " is null", "null member");
                    return;
                }

                PropertyGrid newGrid = 
                    GuiManager.ObjectDisplayManager.GetObjectDisplayerForObject(result, this, pwa.MemberName) as PropertyGrid;

                #region If able to get the result object successfully and it's not null

                if (result != null)
                {

                    pwa.ChildFloatingWindow = newGrid;

                    pwa.ChildFloatingWindow.Closing += RemoveSelfFromPwaChildFloatingWindow;

                    newGrid.Name = pwa.MemberName;
                    newGrid.ContentManagerName = this.ContentManagerName;

                    pwa.OnMemberChanged(newGrid, this);
                }
                #endregion

            }
            #endregion
        }


        private void CreateWindow(PropertyWindowAssociation pwa, bool callUpdateDisplayedProperties)
        {
            #region enum - ComboBox

            if (pwa.Type.IsEnum)
            {
                ComboBox comboBox = new ComboBox(mCursor);
                AddWindowBase(comboBox);
                comboBox.ScaleX = 5;
                pwa.Window = comboBox;



                SetEnumerationValuesForComboBox(pwa);

                comboBox.ItemClick += ChangeEnumValue;
                if (SortEnumerations)
                {
                    comboBox.SortingStyle = ListBoxBase.Sorting.AlphabeticalIncreasing;
                }            
            
            }

            #endregion

            #region bool - ComboBox

            else if (pwa.Type == typeof(bool))
            {
                ComboBox comboBox = new ComboBox(mCursor);
                AddWindowBase(comboBox);
                pwa.Window = comboBox;
                comboBox.AddItem("true", true);
                comboBox.AddItem("false", false);
                comboBox.ItemClick += ChangeBoolValue;
                comboBox.ScaleX = 4.5f;
            }

            #endregion

            #region byte - UpDown
            else if (pwa.Type == typeof(byte))
            {
                UpDown upDown = new UpDown(mCursor);
                AddWindowBase(upDown);
                pwa.Window = upDown;

                upDown.ValueChanged += ChangeByteValue;
                // If we use LosingFocus I think we get 2 events called when the user
                // presses enter.
                // upDown.LosingFocus += ChangeByteValue;

                upDown.AfterValueChanged += StoreUndoByte;

                upDown.MinValue = byte.MinValue;
                upDown.MaxValue = byte.MaxValue;

                upDown.ScaleX = 5;
            }
            #endregion

            #region float - UpDown
            else if (pwa.Type == typeof(float))
            {
                UpDown upDown = new UpDown(mCursor);
                AddWindowBase(upDown);
                pwa.Window = upDown;

                upDown.ValueChanged += ChangeFloatValue;

                // If we use LosingFocus I think we get 2 events called when the user
                // presses enter.
                // upDown.LosingFocus += ChangeFloatValue;

                upDown.AfterValueChanged += StoreUndoFloat;

                upDown.ScaleX = 5;
            }
            #endregion

            #region double - UpDown
            else if (pwa.Type == typeof(double))
            {
                UpDown upDown = new UpDown(mCursor);
                AddWindowBase(upDown);
                pwa.Window = upDown;

                upDown.ValueChanged += ChangeDoubleValue;
                upDown.LosingFocus += ChangeDoubleValue;

                upDown.AfterValueChanged += StoreUndoDouble;

                upDown.ScaleX = 5;
            }
            #endregion

            #region Vector2 - Vector3Display (modified)

            else if (pwa.Type == typeof(Vector2))
            {
                Vector3Display vector3Display = new Vector3Display(mCursor);
                vector3Display.NumberOfComponents = 2;
                this.AddWindowBase(vector3Display);

                pwa.Window = vector3Display;

                vector3Display.ValueChanged += ChangeVector2Value;
                vector3Display.LosingFocus += ChangeVector2Value;
                vector3Display.AfterValueChanged += StoreUndoVector2;
            }
            #endregion

            #region Vector3 - Vector3Display
            else if (pwa.Type == typeof(Vector3))
            {
                Vector3Display vector3Display = new Vector3Display(mCursor);
                this.AddWindowBase(vector3Display);

                pwa.Window = vector3Display;

                vector3Display.ValueChanged += ChangeVector3Value;
                vector3Display.LosingFocus += ChangeVector3Value;
                vector3Display.AfterValueChanged += StoreUndoVector3;
            }
            #endregion

            #region Color - ColorDisplay
            else if (pwa.Type == typeof(Color))
            {
                ColorDisplay colorDisplay = new ColorDisplay(mCursor);
                this.AddWindowBase(colorDisplay);

                pwa.Window = colorDisplay;

                colorDisplay.ValueChanged += ChangeColorValue;
                colorDisplay.LosingFocus += ChangeColorValue;

            }
            #endregion

            #region char - TextBox

            else if (pwa.Type == typeof(char))
            {
                TextBox textBox = new TextBox(mCursor);
                AddWindowBase(textBox);
                pwa.Window = textBox;
                textBox.ScaleX = mNewTextBoxScale;
                textBox.LosingFocus += StoreUndoChar;
                textBox.LosingFocus += ChangeCharValue;
                textBox.TextChange += delegate
                {
                    if (textBox.Text.Length > 1)
                    {
                        char t = textBox.Text[1];
                        textBox.Text = "";
                        textBox.Text += t;
                    }
                };
                
            }
            #endregion

            #region string - TextBox

            else if (pwa.Type == typeof(string))
            {
                TextBox textBox = new TextBox(mCursor);
                AddWindowBase(textBox);
                pwa.Window = textBox;
                textBox.ScaleX = mNewTextBoxScale;
                textBox.Resizing += ExtraWindowResize;
                textBox.LosingFocus += StoreUndoString;
                textBox.LosingFocus += ChangeStringValue;

            }
            #endregion

            #region long - TextBox

            else if (pwa.Type == typeof(long))
            {
                TextBox textBox = new TextBox(mCursor);
                AddWindowBase(textBox);
                pwa.Window = textBox;
                textBox.ScaleX = 7;
                textBox.LosingFocus += ChangeLongValue;
                textBox.Format = TextBox.FormatTypes.Integer;

            }

            #endregion

            #region Texture - Button
            else if (pwa.Type == typeof(Texture2D))
            {
                Button button = new Button(mCursor);
                AddWindowBase(button);
                pwa.Window = button;
                button.ScaleX = 5;
                button.ScaleY = 5;

                button.Click += this.ChangeTexture;
                button.SecondaryClick += this.ShowRightClickMenu;
            }
            #endregion

            #region int - UpDown
            else if (pwa.Type == typeof(int))
            {
                UpDown upDown = new UpDown(mCursor);
                AddWindowBase(upDown);
                pwa.Window = upDown;
                upDown.Precision = 0;

                upDown.ValueChanged += ChangeIntValue;
                upDown.LosingFocus += ChangeIntValue;

                upDown.AfterValueChanged += StoreUndoInt;

                upDown.ScaleX = 5;
            }
            #endregion

            #region short - UpDown
            else if (pwa.Type == typeof(short))
            {
                UpDown upDown = new UpDown(mCursor);
                AddWindowBase(upDown);
                pwa.Window = upDown;
                upDown.Precision = 0;

                upDown.ValueChanged += ChangeShortValue;
                upDown.LosingFocus += ChangeShortValue;

                upDown.AfterValueChanged += StoreUndoShort;

                upDown.ScaleX = 5;
            }

            #endregion


            #region BitmapFont - Button that brings up PropertyGrid for new font

            else if (pwa.Type == typeof(BitmapFont))
            {
                Button button = new Button(mCursor);
                AddWindowBase(button);
                pwa.Window = button;
                button.ScaleX = 6;
                button.ScaleY = 1.2f;
                pwa.IsViewable = true;

                if (pwa.CanWrite)
                {
                    button.Text = "Edit Property";

                    button.Click += OpenChangeBitmapFontWindow;
                }
                else
                {
                    button.Text = "View Property";
                    button.Click += this.EditPropertyPress;
                }



            }

            #endregion

            #region All Other Types - Button
            else
            {
                // make sure that the item should be created
                Button button = new Button(mCursor);
                AddWindowBase(button);
                pwa.Window = button;
                button.ScaleX = 6;
                button.ScaleY = 1.2f;

                if (pwa.CanWrite)
                {
                    button.Text = "Edit Property";
                }
                else
                {
                    button.Text = "View Property";
                }

                pwa.IsViewable = true;

                button.Click += this.EditPropertyPress;
            }
            #endregion



            if (pwa.Window != null)
            {
                #region Create a label
                TextDisplay textDisplay = new TextDisplay(mCursor);
                AddWindowBase(textDisplay);
                pwa.Label = textDisplay;
                textDisplay.Text = pwa.MemberName;
                #endregion

                if ((!pwa.CanWrite && !pwa.IsViewable) || this.Enabled == false)
                    pwa.Window.Enabled = false;

                if (mSelectedObject != null && callUpdateDisplayedProperties)
                {
                    UpdateDisplayedProperties();

                }

            }

        }


        private bool DoesTypeOpenNewPropertyGrid(Type type)
        {

            return !(
                type.IsEnum ||
                type == typeof(byte) ||
                type == typeof(bool) ||
                type == typeof(float) ||
                type == typeof(double) ||
                type == typeof(string) ||
                type == typeof(Texture2D) ||
                type == typeof(int) ||
                type == typeof(short) ||
                type == typeof(long) ||
                type == typeof(Vector2) ||
                type == typeof(Vector3) ||
                type == typeof(Vector4) ||
                type == typeof(Color) ||
                type == typeof(BitmapFont)
            );

        }


        protected int IndexOfWindow(IWindow windowInQuestion)
        {
            for (int i = 0; i < mPropertyWindowAssociations.Count; i++)
            {
                if (mPropertyWindowAssociations[i].Window == windowInQuestion)
                    return i;
            }
            return -1;
        }


        private bool IsUserEditingWindow(IWindow windowInQuestion)
        {
            if (windowInQuestion is ListDisplayWindow)
                return false;

            return ((Window)windowInQuestion).IsWindowOrChildrenReceivingInput ||
                (windowInQuestion is Vector3Display && ((Vector3Display)windowInQuestion).IsUserEditingWindow);
        }


        private void RefreshFloatingChildrenReferences()
        {
            for (int index = 0; index < mPropertyWindowAssociations.Count; index++)
            {
                if (mPropertyWindowAssociations[index].ChildFloatingWindow != null)
                {
                    #region Get the object and stuff it in the "result" local variable

                    // Refresh the property:
                    BindingFlags flags = GetGetterBindingFlag(mPropertyWindowAssociations[index]);
                    Binder binder = null;
                    object[] args = null;

                    object result = mSelectedType.InvokeMember(
                       mPropertyWindowAssociations[index].MemberName,
                       flags,
                       binder,
                       mSelectedObject,
                       args
                       );
                    #endregion

                    if (mPropertyWindowAssociations[index].ChildFloatingWindow is PropertyGrid)
                    {


                        mPropertyWindowAssociations[index].SetPropertyFor(
                            mPropertyWindowAssociations[index].ChildFloatingWindow as PropertyGrid, result);
                    }
                    else if (mPropertyWindowAssociations[index].ChildFloatingWindow is ListDisplayWindow)
                    {
                        // refresh the list shown
                        (mPropertyWindowAssociations[index].ChildFloatingWindow as ListDisplayWindow).ListShowing =
                            result as IEnumerable;

                    }



                }
            }
        }


        private void SetEnumerationValuesForComboBox(PropertyWindowAssociation pwa)
        {
#if XBOX360 || WINDOWS_PHONE || MONODROID
            throw new NotImplementedException(
                "This isn't implemented due to the compact framework limitations on the Enum class.  Can prob implement this using reflection if necessary.");

#else
            ComboBox comboBox = pwa.Window as ComboBox;

            comboBox.Clear();

            string[] availableValues = Enum.GetNames(pwa.Type);
            Array array = Enum.GetValues(pwa.Type);

            int i = 0;

            foreach (object enumValue in array)
            {
                string s = availableValues[i];

                bool shouldAdd = true;

                if (mExcludedEnumerationValues.ContainsKey(pwa.Type) &&
                    mExcludedEnumerationValues[pwa.Type].Contains(s))
                {
                    shouldAdd = false;
                }

                if (shouldAdd)
                {
                    comboBox.AddItem(s, enumValue);
                }
                i++;
            }
#endif
        }


        private void SetMemberTexture2D(string memberName, Texture2D textureToSet)
        {
            PropertyWindowAssociation pwa = GetPropertyWindowAssociationForMember(memberName);

            if (this.mSelectedType.IsValueType)
            {
#if XBOX360 || WINDOWS_PHONE || MONODROID
                throw new NotImplementedException();
#else
                FieldInfo fieldInfo = mSelectedType.GetField(
                    pwa.MemberName);

                fieldInfo.SetValueDirect(
                    __makeref(mSelectedObject), textureToSet);
#endif
            }
            else
            {
                object[] args = { textureToSet };

                mSelectedType.InvokeMember(memberName,
                    GetSetterBindingFlag(pwa), null, mSelectedObject, args);
            }

            ((Button)pwa.Window).SetOverlayTextures(textureToSet, null);

            if(textureToSet == null)
            {
                ((Button)pwa.Window).Text = "null\nTexture";
            }

            // Perform any custom behavior related to this member being changed
            pwa.OnMemberChanged(
                pwa.Window, this);
        }


        private void UpdateTabOrder()
        {
            // TODO:  Complete here
            if (mWavListBox != null)
            {
                // There are categories so use the WavListBox to update
                // tab order

                for (int i = 0; i < mWavListBox.Count; i++)
                {
                    CollapseItem item = mWavListBox[i];

                    IInputReceiver inputReceiver = null;
                    WindowArray windowArray = item.ReferenceObject as WindowArray;

                    foreach (Window window in windowArray)
                    {
                        if (window is IInputReceiver)
                        {
                            if (inputReceiver != null)
                            {
                                inputReceiver.NextInTabSequence = window as IInputReceiver;
                            }

                            inputReceiver = window as IInputReceiver;
                        }
                    }
                }
            }
            else
            {
                IInputReceiver inputReceiver = null;
                // There are no categories so use the PropertyWindowArray order
                // to create the tab order.
                foreach (PropertyWindowAssociation pwa in mPropertyWindowAssociations)
                {
                    if (pwa.Window is IInputReceiver)
                    {
                        if (inputReceiver != null)
                        {
                            inputReceiver.NextInTabSequence = pwa.Window as IInputReceiver;
                        }
                        inputReceiver = pwa.Window as IInputReceiver;
                    }
                }
            }

        }


        internal override void UpdateUIAndDisplayedProperties()
        {
            UpdateUI();

            // now update the values of the UI
            UpdateDisplayedProperties();
        }

        internal override void UpdateUI()
        {
            if (mPropertyWindowAssociations.Count == 0)
            {
                CreateWindowsForSelectedType();
            }
            else
            {
                UpdateScaleAndWindowPositions();
            }

            bool windowsEnabled = mSelectedObject != null;

            if (windowsEnabled)
            {
                foreach (PropertyWindowAssociation pwa in mPropertyWindowAssociations)
                {
                    if (this.mChildren.Contains(pwa.Window))
                        pwa.Window.Enabled = pwa.CanWrite && this.Enabled;
                }
            }
            else
            {
                foreach (PropertyWindowAssociation pwa in mPropertyWindowAssociations)
                {
                    pwa.Window.Enabled = false || pwa.IsStatic;
                }
            }

            RefreshFloatingChildrenReferences();
        }

        // To eliminate GC
        Dictionary<string, Vector2> mScaleForEachCategory = new Dictionary<string, Vector2>();
        Dictionary<string, float> mLargestLabelWidth = new Dictionary<string, float>();
        public override void UpdateScaleAndWindowPositions()
        {
            float startingX = .5f;
            float nextY = 1;
            const float distanceBetweenLabelAndWindow = 1;

            #region If there are no categories
            if (mWavListBox == null)
            {
                float maximumTextDisplayWidth = 0;

                foreach (PropertyWindowAssociation pwa in mPropertyWindowAssociations)
                {
                    if (pwa.Label != null)
                    {
                        maximumTextDisplayWidth = System.Math.Max(maximumTextDisplayWidth,
                            pwa.Label.Width);
                    }
                }

                float maximumWindowWidth = 0;


                foreach (PropertyWindowAssociation pwa in mPropertyWindowAssociations)
                {
                    if (pwa.Window != null)
                    {
                        float windowScale = pwa.Window.ScaleY;

                        if (pwa.Label != null)
                        {
                            pwa.Label.X = startingX;
                            pwa.Label.Y = nextY + windowScale;
                        }

                        pwa.Window.X = distanceBetweenLabelAndWindow + startingX + maximumTextDisplayWidth + pwa.Window.ScaleX;
                        pwa.Window.Y = nextY + windowScale;

                        nextY += windowScale * 2.0f;

                        maximumWindowWidth = System.Math.Max(maximumWindowWidth,
                            pwa.Window.ScaleX);
                    }
                }



                SetScaleTL(maximumTextDisplayWidth / 2.0f + maximumWindowWidth + 2, .25f + nextY / 2.0f, true);
            }
            #endregion

            #region Else there are categories
            else
            {
                startingX = mWavListBox.ScaleX * 2 + 1;
                const float amountToShiftForNoLabel = -1;

                #region Get the largest lable widths used for setting the PropertyGrid's ScaleX

                mLargestLabelWidth.Clear();
                foreach (PropertyWindowAssociation pwa in mPropertyWindowAssociations)
                {
                    if (pwa.Label != null)
                    {
                        string category = pwa.Category;
                        if (mLargestLabelWidth.ContainsKey(pwa.Category) == false)
                        {
                            mLargestLabelWidth.Add(pwa.Category, 0.0f);
                        }

                        mLargestLabelWidth[pwa.Category] = System.Math.Max(mLargestLabelWidth[pwa.Category], pwa.Label.Width);
                    }
                }

                #endregion

                mScaleForEachCategory.Clear();

                foreach (PropertyWindowAssociation pwa in mPropertyWindowAssociations)
                {
                    // Loop through all windows, find out which category they're in, then adjust the
                    // scale for that category
                    if (pwa.Window != null)
                    {
                        // If this category hasn't been visited yet, make the vector.
                        if (mScaleForEachCategory.ContainsKey(pwa.Category) == false)
                        {
                            mScaleForEachCategory.Add(pwa.Category, new Vector2(0, 0.3f));
                        }
                        if (mLargestLabelWidth.ContainsKey(pwa.Category) == false)
                        {
                            mLargestLabelWidth.Add(pwa.Category, 0);
                        }

                        Vector2 scaleVector = mScaleForEachCategory[pwa.Category];

                        nextY = scaleVector.Y * 2;

                        float windowScale = pwa.Window.ScaleY;
                        if (pwa.Label != null)
                        {
                            pwa.Label.X = startingX;
                            pwa.Label.Y = nextY + windowScale;

                            if (string.IsNullOrEmpty(pwa.Label.Text))
                            {
                                pwa.Window.X = startingX + mLargestLabelWidth[pwa.Category] +
                                    distanceBetweenLabelAndWindow + pwa.Window.ScaleX + amountToShiftForNoLabel;

                            }
                            else
                            {
                                pwa.Window.X = startingX + mLargestLabelWidth[pwa.Category] +
                                    distanceBetweenLabelAndWindow + pwa.Window.ScaleX;
                            }
                        }
                        else
                        {
                            pwa.Window.X = startingX + mLargestLabelWidth[pwa.Category] + pwa.Window.ScaleX;

                        }
                        pwa.Window.Y = nextY + windowScale;

                        scaleVector.X = System.Math.Max((pwa.Window.X + pwa.Window.ScaleX + .5f) / 2.0f, scaleVector.X);

                        mScaleForEachCategory[pwa.Category] = new Vector2(scaleVector.X, scaleVector.Y + windowScale);
                    }
                }

                #region Loop through scaleForEachCategory and assign the category scale in the mWavListBox
                foreach (KeyValuePair<string, Vector2> kvp in mScaleForEachCategory)
                {
                    if (kvp.Key == "")
                    {
                        mWavListBox.SetCategoryScale(
                            mUncategorizedCategoryName,
                            kvp.Value.X,
                            kvp.Value.Y + .3f);
                    }
                    else
                    {
                        mWavListBox.SetCategoryScale(
                            kvp.Key,
                            kvp.Value.X,
                            kvp.Value.Y + .3f);
                    }
                }
                #endregion

                #region Refresh the scale by simulating a click of the category

                CollapseItem item = mWavListBox.GetFirstHighlightedItem();
                if (item != null)
                {
                    mWavListBox.HighlightItem(item.Text);
                }
                #endregion
            }
            #endregion
        }


        public override void UpdateWavMembership()
        {
            if (mWavListBox != null)
            {
                // If the WavListBox is not null,
                // then all Windows must belong either
                // to their own category or to the default
                // <Uncategorized> category
                foreach (PropertyWindowAssociation pwa in mPropertyWindowAssociations)
                {
					UpdateWavMembership(pwa);
                }
            }
        }

		internal override void UpdateWavMembership(PropertyWindowAssociation pwa)
		{
			if (mWavListBox != null && pwa.Window != null)
			{
				if (pwa.Category != "")
				{
					if (mWavListBox.IsWindowInCategory(pwa.Window, pwa.Category) == false)
					{
						if (mWavListBox.GetObject(pwa.Category) == null)
						{
							throw new System.NullReferenceException("There is no " + pwa.Category + " category to add the floating window.");
						}

						mWavListBox.AddWindowToCategory(pwa.Window, pwa.Category);


					}

					if (pwa.Label != null && ! mWavListBox.IsWindowInCategory(pwa.Label, pwa.Category))
					{
						if (mWavListBox.GetObject(pwa.Category) == null)
						{
							throw new System.NullReferenceException("There is no " + pwa.Category + " category to add the floating window.");
						}
						mWavListBox.AddWindowToCategory(pwa.Label, pwa.Category);
					}					
				}
				else
				{
					if (mWavListBox.IsWindowInCategory(pwa.Window, mUncategorizedCategoryName) == false)
					{
						if (mWavListBox.GetObject(mUncategorizedCategoryName) == null)
						{
							throw new System.NullReferenceException("There is no Uncategorized category to add the member " + pwa.MemberName + ".");
						}
						mWavListBox.AddWindowToCategory(pwa.Window, mUncategorizedCategoryName);

						if (pwa.Label != null)
						{
							mWavListBox.AddWindowToCategory(pwa.Label, mUncategorizedCategoryName);
						}
					}
				}
			}
		}


        #endregion

        #endregion

        #region IObjectDisplayer<T> Members

        public T ObjectDisplaying
        {
            get
            {
                return mSelectedObject;

            }
            set
            {
                mSelectedObject = value;
                UpdateUIAndDisplayedProperties();   
            }
        }

        #endregion
    }
#endif

}
