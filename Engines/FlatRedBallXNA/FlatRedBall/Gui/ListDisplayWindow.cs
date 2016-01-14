using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;

using FlatRedBall.Math;
using System.Reflection;
using FlatRedBall.Instructions;
using FlatRedBall.Utilities;
#if !SILVERLIGHT
using FlatRedBall.Gui.PropertyGrids;
#endif

#if FRB_MDX
using Keys = Microsoft.DirectX.DirectInput.Key;
#elif FRB_XNA
using Keys = Microsoft.Xna.Framework.Input.Keys;
#endif


namespace FlatRedBall.Gui
{  

    public class ListDisplayWindow : Window, IObjectDisplayer<IEnumerable>
    {
        #region Fields



        Button mRemovalButton;
        Button mAdditionButton;
        IEnumerable mListShowing;
        IList mAsIList;
#if !SILVERLIGHT

        CollapseListBox mListBox;
#endif

        bool mShowPropertyGridOnStrongSelect;
#if !SILVERLIGHT
        #region XML Docs
        /// <summary>
        /// List of windows which are always shown.  This can be used to modify the ListDisplayWindow
        /// to support more windows and behavior.
        /// </summary>
        #endregion
        WindowArray mExtraChildrenWindows = new WindowArray();

        string mContentManagerName = null;

        bool mConsiderAttachments;

        string mMemberDisplaying;

        Type mTypeOfObjectInList;

        /// <summary>
        /// This must be set prior to calling ShowCreationWindow if element creation
        /// is to be handled by FlatRedBall.
        /// </summary>
        List<Type> mTypeList;

        #region XML Docs
        /// <summary>
        /// The list that the Grid's UndoInstructions are set to.
        /// </summary>
        #endregion
        List<InstructionList> mUndoInstructions;

        float mNewGridX = float.NaN;
        float mNewGridY = float.NaN;

        object mLastItemRemoved;
        object mLastItemAdded;
        Window mLastChildWindowCreated;

        List<object> mLastObjectsPasted = new List<object>();

        private CreationWindow mCreationWindow;
 #endif

        bool mAllowCut = false;
        static List<object> mObjectsCopied = new List<object>();

        List<object> mOptions = new List<object>();

        #endregion

        #region Properties


        #region XML Docs
        /// <summary>
        /// Whether the ListDisplayWindow has CTRL+X cut and CTRL+V paste functionality.
        /// </summary>
        #endregion
        public bool AllowCut
        {
            get { return mAllowCut; }
            set { mAllowCut = value; }
        }


        public bool AllowCopy
        {
            get;
            set;
        }


        public bool AllowItemDragDrop
        {
            get;
            set;
        }

#if !SILVERLIGHT

        public bool AllowReordering
        {
            get { return mListBox.AllowReordering; }
            set { mListBox.AllowReordering = value; }
        }

        public bool AllowCtrlClick
        {
            get { return mListBox.CtrlClickOn; }
            set { mListBox.CtrlClickOn = value; }
        }

        public bool AllowShiftClick
        {
            get { return mListBox.ShiftClickOn; }
            set { mListBox.ShiftClickOn = value; }
        }

        #region XML Docs
        /// <summary>
        /// Whether the ListDisplayWindow attempts to display attachments.  If the list
        /// being displays contains IAttachables and this is true then the tree structure
        /// will be rerpesented in the CollapseListBox.  Default is false.
        /// </summary>
        #endregion
        public bool ConsiderAttachments
        {
            get { return mConsiderAttachments; }
            set { mConsiderAttachments = value; }
        }

        
        public string ContentManagerName
        {
            get { return mContentManagerName; }
            set { mContentManagerName = value; }
        }

        
        public bool DrawOuterWindow
        {
            get { return this.DrawBorders; }
            set { this.DrawBorders = value; }
        }


        public List<Keys> IgnoredKeys
        {
            get { return mListBox.IgnoredKeys; }
        }

#endif

        public List<CollapseItem> Items
        {
            get 
            {
#if SILVERLIGHT
                throw new NotImplementedException();
#else
                return mListBox.Items; 
#endif
            }
        }

#if !SILVERLIGHT
        public object LastItemAdded
        {
            get { return mLastItemAdded; }
        }


        public object LastItemRemoved
        {
            get { return mLastItemRemoved; }
        }


        public List<object> LastItemsPasted
        {
            get
            {
                return mLastObjectsPasted;
            }
        }


        public Window LastChildWindowCreated
        {
            get { return mLastChildWindowCreated; }
        }

        
        public bool Lined
        {
            get { return mListBox.Lined; }
            set { mListBox.Lined = value; }
        }

        public CollapseListBox ListBox
        {
            get { return mListBox; }
        }
#endif
        public IEnumerable ListShowing
        {
            get { return mListShowing; }
            set 
            {
				bool needToCollapse = false;
                if (mListShowing != null && mListShowing != value)
                {
                    // changing to a different list - hide all the PropertyGrids
                    while (mFloatingWindows.Count != 0)
                    {
                        GuiManager.RemoveWindow(mFloatingWindows[0]);
                    }

					needToCollapse = true;
                }
                mListShowing = value;
                mAsIList = mListShowing as IList;

#if SILVERLIGHT || WINDOWS_PHONE || MONODROID
                throw new NotImplementedException();
#else
                UpdateToList(); 
                // CK - Moving NeedToCollapse into non-SL/Phone section as mListBox isn't defined here.
				if (needToCollapse)
				{
					for (int i = 0; i < mListBox.Items.Count; i++)
					{
						mListBox.Items[i].Collapse();
					}
				}
#endif
            }

        }

        public int MaximumElements
        {
            get;
            set;
        }
#if !SILVERLIGHT

        #region XML Docs
        /// <summary>
        /// If this ListDisplayWindow was created by a PropertyGrid then
        /// it is displaying a member that is a List.  MemberDisplaying is
        /// the string name of the member being displayed.  This defaults
        /// to null if this Window was not created by a PropertyGrid.
        /// </summary>
        #endregion
        public string MemberDisplaying
        {
            get
            {
                return mMemberDisplaying;
            }
            set
            {
                mMemberDisplaying = value;
            }

        }


        public float NewGridX
        {
            get { return mNewGridX; }
            set { mNewGridX = value; }
        }


        public float NewGridY
        {
            get { return mNewGridY; }
            set { mNewGridY = value; }
        }
#endif

        public bool PrependIndex
        {
            get;
            set;
        }


        public bool ShowPropertyGridOnStrongSelect
        {
            get { return mShowPropertyGridOnStrongSelect; }
            set { mShowPropertyGridOnStrongSelect = value; }
        }
#if !SILVERLIGHT

        public List<InstructionList> UndoInstructions
        {
            set { mUndoInstructions = value; }
        }


        public GuiMessage ShowCreationWindow
        {
            get { return AddCreationWindow; }
        }


        public List<Type> CreationTypes
        {
            get { return mTypeList; }
            set { mTypeList = value; }
        }
#endif
        #endregion

        #region Event

        public event GuiMessage AfterAddItem;
        public event GuiMessage AfterItemRemoved;

        public event GuiMessage AfterItemsPasted;

        public event GuiMessage AfterChildWindowCreated;

        #endregion

        #region Event Methods
#if !SILVERLIGHT

        private void AddCreationWindow(Window callingWindow)
        {
            if (mTypeList != null && !mCreationWindow.Visible)
            {
                mCreationWindow.Types = mTypeList;
                GuiManager.AddDominantWindow(mCreationWindow);

                mCreationWindow.X = this.ScreenRelativeX;
                mCreationWindow.Y = this.ScreenRelativeY + this.ScaleY - mCreationWindow.ScaleY;
                mCreationWindow.Visible = true;

            }
        }

        private void AddItemButtonClick(Window callingWindow)
        {
            if (CreationTypes != null)
            {
                ShowCreationWindow(callingWindow);

            }



            else
            {

                if (mListShowing is IList)
                {
                    if (mAsIList.Count >= MaximumElements)
                    {
                        GuiManager.ShowMessageBox("The list cannot have any more elements added to it.", "Error Adding");
                    }
                    else
                    {

                        if (mTypeOfObjectInList == typeof(string))
                        {
                            mAsIList.Add("");
#if !WINDOWS_PHONE
                            UpdateToList();
#endif
                        }
                        else
                        {
                            AddExistingInstance(Activator.CreateInstance(mTypeOfObjectInList));
                        }
                    }
                }
                else // see if we can find an Add method
                {

                    MethodInfo methodInfo = mListShowing.GetType().GetMethod("Add");

                    if(methodInfo == null)
                    {
                        throw new InvalidOperationException("There's no Add method in the type " + mListShowing.GetType().Name);
                    }
                    else
                    {
                        object newObject = Activator.CreateInstance(mTypeOfObjectInList);

                        mLastItemAdded = newObject;

                        methodInfo.Invoke(mListShowing, new object[] { newObject });

                        if (AfterAddItem != null)
                        {
                            AfterAddItem(this);
                        }
#if !WINDOWS_PHONE
                        UpdateToList();
#endif
                    }
                }
            }
        }

        private void CloseChildWindow(Window callingWindow)
        {
            mNewGridX = callingWindow.X - callingWindow.ScaleX;
            mNewGridY = callingWindow.Y - callingWindow.ScaleY;

            GuiManager.RemoveWindow(callingWindow);
        }

        private void AddExistingInstance(object toInsert)
        {
            mLastItemAdded = toInsert;

            if (mAsIList != null)
            {
                if (mAsIList.IsReadOnly)
                {
                    GuiManager.ShowMessageBox("This list is read-only.  Cannot add.", "List is read-only");
                    return;
                }
                this.mAsIList.Add(mLastItemAdded);
            }
            else
            {
                MethodInfo methodInfo = mListShowing.GetType().GetMethod("Add");

                if (methodInfo == null)
                {
                    throw new InvalidOperationException("There's no Add method in the type " + mListShowing.GetType().Name);
                }
                else
                {
                    methodInfo.Invoke(mListShowing, new object[] { mLastItemAdded });
                }
            }

            if (AfterAddItem != null)
            {
                AfterAddItem(this);
            }
#if !WINDOWS_PHONE
            UpdateToList();
#endif
        }


        private void ItemStrongSelect(Window callingWindow)
        {
            if (mShowPropertyGridOnStrongSelect)
            {
                ShowPropertyGridForSelectedObject();
            }
        }

        private void OnFocusUpdate(IInputReceiver inputReceiver)
        {
#if FRB_MDX
            bool deletePressed = Input.InputManager.Keyboard.KeyPushed(Microsoft.DirectX.DirectInput.Key.Delete);
#elif FRB_XNA
            bool deletePressed = Input.InputManager.Keyboard.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Delete);
#endif

            if (deletePressed && mRemovalButton != null)
            {
                RemoveItemButtonClick(null);
                //mRemovalButton.OnClick();
            }

            #region CTRL+X
            if (Input.InputManager.Keyboard.ControlXPushed() && mAllowCut)
            {
                mObjectsCopied.Clear();

                List<CollapseItem> highlightedItems = mListBox.GetHighlightedItems();

                foreach (CollapseItem item in highlightedItems)
                {
                    mObjectsCopied.Add(item.ReferenceObject);
                }

                RemoveItemButtonClick(null);
                HighlightItem(null, false);
            }
            #endregion

            #region CTRL+C, ctrl c

            if (Input.InputManager.Keyboard.ControlCPushed() && AllowCopy)
            {
                mObjectsCopied.Clear();

                List<CollapseItem> highlightedItems = mListBox.GetHighlightedItems();

                foreach (CollapseItem item in highlightedItems)
                {
                    mObjectsCopied.Add(item.ReferenceObject);
                }
            }

            #endregion

            #region CTRL+V
            if (Input.InputManager.Keyboard.ControlVPushed() && (mAllowCut || AllowCopy))
            {
                CollapseItem highlightedItem = GetFirstHighlightedItem();

                List<Object> newlyCreatedObjects = new List<object>();

                foreach (object o in mObjectsCopied)
                {

                    if (o is ICloneable)
                    {
                        object newObject = ((ICloneable)o).Clone();
                        newlyCreatedObjects.Add(newObject);
                    }
                    else
                    {
                        // See if the object defines its own Clone method
                        MethodInfo methodInfo = o.GetType().GetMethod("Clone");

                        if (methodInfo != null)
                        {
                            object newObject = methodInfo.Invoke(o, new object[0]);
                            newlyCreatedObjects.Add(newObject);

                        }
                        else
                        {
                            newlyCreatedObjects.Add(o);
                        }
                    }
                }

                bool wasAdded = false;

                #region Is the highlighted item a child of a list?  If so, add the pasted object in the highlighted parent's list

                if (highlightedItem != null &&
                    highlightedItem.ReferenceObject != null &&
                    highlightedItem.ParentItem != null &&
                    highlightedItem.ParentItem.ReferenceObject is IEnumerable)
                {
                    Type typeOfItemInList = highlightedItem.ReferenceObject.GetType();

                    if (typeOfItemInList.IsAssignableFrom(mObjectsCopied[0].GetType()))
                    {
                        wasAdded = true;
                        foreach (object o in newlyCreatedObjects)
                        {
                            if(highlightedItem.ParentItem.ReferenceObject is IList)
                            {
                                ((IList)highlightedItem.ParentItem.ReferenceObject).Add(o);
                            }
                            else
                            {
                                MethodInfo methodInfo = 
                                    highlightedItem.ParentItem.ReferenceObject.GetType().GetMethod("Add");

                                methodInfo.Invoke(
                                    highlightedItem.ParentItem.ReferenceObject, 
                                    new object[] { o });


                            }
                        }
                    }
                }
                #endregion

                if (!wasAdded && highlightedItem != null && highlightedItem.ReferenceObject != null && highlightedItem.ReferenceObject is IEnumerable)
                {
                    Type typeOfItemInList = GetTypeOfObjectInList(highlightedItem.ReferenceObject.GetType());

                    if (typeOfItemInList.IsAssignableFrom(mObjectsCopied[0].GetType()))
                    {
                        wasAdded = true;

                        foreach (object o in newlyCreatedObjects)
                        {
                            if(highlightedItem.ReferenceObject is IList)
                            {
                                ((IList)highlightedItem.ReferenceObject).Add(o);
                            }
                            else
                            {
                                MethodInfo methodInfo = 
                                    highlightedItem.ReferenceObject.GetType().GetMethod("Add");

                                methodInfo.Invoke(
                                    highlightedItem.ReferenceObject, 
                                    new object[] { o });
                            }
                        }
                    }
                }


                if (!wasAdded && mAsIList != null && mObjectsCopied.Count != 0 && mTypeOfObjectInList != null)
                {

                    // See if the object can be added
                    if (mTypeOfObjectInList.IsAssignableFrom(mObjectsCopied[0].GetType()))
                    {
                        wasAdded = true;

                        foreach (object o in newlyCreatedObjects)
                        {
                            mAsIList.Add(o);
                            mLastItemAdded = o;

                            if (AfterAddItem != null)
                            {
                                AfterAddItem(this);
                            }
                        }

#if !WINDOWS_PHONE
                        UpdateToList();
#endif
                        HighlightObject(null, false);

                        foreach (object o in mObjectsCopied)
                        {
                            HighlightObject(o, true);
                        }
                    }
                }

                mLastObjectsPasted.Clear();
                mLastObjectsPasted.AddRange(newlyCreatedObjects);

                if (AfterItemsPasted != null)
                {
                    AfterItemsPasted(this);
                }


            }
            #endregion

        }

        private void RemoveItemButtonClick(Window callingWindow)
        {
            List<CollapseItem> itemsToRemove = mListBox.GetHighlightedItems();

            for(int i = itemsToRemove.Count - 1;  i > -1; i--)
            {
                CollapseItem item = itemsToRemove[i];

                object highlightedObject = item.ReferenceObject;

                mLastItemRemoved = highlightedObject;

                if (highlightedObject != null)
                {
                    if (item.ParentItem != null)
                    {
                        IList parentObjectAsList = item.ParentItem.ReferenceObject as IList;

                        if (parentObjectAsList != null)
                        {
                            parentObjectAsList.Remove(highlightedObject);
                        }
                        else
                        {
                            MethodInfo methodInfo = item.ParentItem.ReferenceObject.GetType().GetMethod("Remove");

                            methodInfo.Invoke(item.ParentItem.ReferenceObject, new object[] { highlightedObject });
                        }
                    }
                    else
                    {
                        int indexInList = mListBox.mItems.IndexOf(item);

                        #region If the referenced enumerable is a IList, then call Remove
                        if (mAsIList != null)
                        {
                            if (mAsIList.IsReadOnly)
                            {
                                GuiManager.ShowMessageBox("This list is read-only.  Cannot remove.", "List is read-only");
                                return;
                            }
                            mAsIList.RemoveAt(indexInList);
                        }
                        #endregion

                        #region else, see if the referenced enumerable has a Remove method
                        else
                        {
                            MethodInfo methodInfo = mListShowing.GetType().GetMethod("RemoveAt");

                            if (methodInfo == null)
                            {
                                throw new InvalidOperationException("There's no RemoveAt method in the type " + mListShowing.GetType().Name);
                            }
                            else
                            {
                                methodInfo.Invoke(mListShowing, new object[] { indexInList });
                            }

                        }
                        #endregion
                    }
                }

#if !WINDOWS_PHONE
                UpdateToList();
#endif

                if (AfterItemRemoved != null)
                {
                    AfterItemRemoved(this);
                }

            }

            HighlightObject(null, false);
        }

        private void ResizeListWindow(Window callingWindow)
        {
            float extraScale = 0;
            float borderScale = 1;

            if (DrawOuterWindow == false)
                borderScale = 0;

            foreach (Window window in mExtraChildrenWindows)
            {
                extraScale += window.ScaleY + .5f; // .5 to space between the windows.
            }


            mListBox.X = this.ScaleX;
            mListBox.Y = this.ScaleY - extraScale;

            mListBox.ScaleX = this.ScaleX - borderScale;
            mListBox.ScaleY = this.ScaleY - borderScale - extraScale;

            float nextY = mListBox.Y + mListBox.ScaleY;

            foreach (Window window in mExtraChildrenWindows)
            {
                nextY += window.ScaleY + .5f;

                window.Y = nextY;
                window.X = window.ScaleX + 1f;
                nextY += window.ScaleY;
            }
        }

        private void ReorderListBox(ReorderValues reorderValues)
        {
            if (reorderValues.OldParent == reorderValues.NewParent)
            {
                if (reorderValues.OldParent != null)
                {
                    if (reorderValues.OldParent.ReferenceObject is IList)
                    {
                        IList parent = reorderValues.OldParent.ReferenceObject as IList;

                        parent.Remove(reorderValues.ItemMoved.ReferenceObject);
                        parent.Insert(reorderValues.NewIndex, reorderValues.ItemMoved.ReferenceObject);

                    }
                }
                else
                {
                    // reordering at the top of the list.
                    IList parent = mAsIList;

                    if (parent != null)
                    {

                        parent.RemoveAt(reorderValues.OldIndex);
                        parent.Insert(reorderValues.NewIndex, reorderValues.ItemMoved.ReferenceObject);
                    }
                }
            }
        }

        private void TestDragDrop(Window callingWindow)
        {

            object referenceObjectDraggedOff = GuiManager.CollapseItemDraggedOff.ReferenceObject;

            if (referenceObjectDraggedOff != null)
            {
                try
                {
                    mAsIList.Add(referenceObjectDraggedOff);
    #if !WINDOWS_PHONE
                    UpdateToList();
    #endif

                    CollapseItem item = mListBox.GetItem(referenceObjectDraggedOff);
                    item.CollapseAll();                
                
                }
                catch (Exception)
                {
                    GuiManager.ShowMessageBox("Could not add " + referenceObjectDraggedOff.ToString() + " to the list", "Error");
                }


            }
        }
#endif

        #endregion

        #region Methods

        #region Constructor

        public ListDisplayWindow(Cursor cursor)
            : base(cursor)
        {
#if SILVERLIGHT
            throw new NotImplementedException();
#else
            MaximumElements = int.MaxValue;

//            Resizable = true;
  //          AddXButton();
    //        MoveBar = true;
            ScaleX = 7;
            ScaleY = 12;
            MinimumScaleX = 4;
            MinimumScaleY = 5;
            Resizing += ResizeListWindow;

            mListBox = new CollapseListBox(mCursor);
            base.AddWindow(mListBox);
            mListBox.StrongSelect += ItemStrongSelect;
            mListBox.SortingStyle = CollapseListBox.Sorting.None;
            mListBox.Reorder += ReorderListBox;
            mListBox.FocusUpdate += OnFocusUpdate;
            mListBox.CollapseItemDropped += TestDragDrop;

            ResizeListWindow(this);
            mCreationWindow = new CreationWindow(cursor);
            mCreationWindow.AddMethod = new CreationWindow.AddDelegate(AddExistingInstance);
            mCreationWindow.Visible = false;

            mListBox.ShowExpandCollapseAllOption = true;

#endif
        }

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
            base.AddWindow(windowToAdd);

            mExtraChildrenWindows.Add(windowToAdd);

            ResizeListWindow(null);
        }

        List<bool> MatchingList = new List<bool>();
        public bool AreHighlightsMatching(IEnumerable collection)
        {
            #region Make MatchingList the same size as mListBox.mHighlightedItems

            while (MatchingList.Count > mListBox.mHighlightedItems.Count)
            {
                MatchingList.RemoveAt(0);
            }
            while(MatchingList.Count < mListBox.mHighlightedItems.Count)
            {
                MatchingList.Add(false);
            }

            #endregion

            #region Reset all matching values to false
            for (int i = 0; i < MatchingList.Count; i++)
            {
                MatchingList[i] = false;
            }
            #endregion

            int collectionCount = 0;

            foreach (object obj in collection)
            {
                collectionCount++;

                // This code was causing a warning.  It's 
                // logically identical to the new code that
                // doesn't cause a warning.
                //for (int i = 0; i < mListBox.mHighlightedItems.Count; i++)
                //{
                //    if(mListBox.mHighlightedItems[i].ReferenceObject == obj)
                //    {
                //        MatchingList[i] = true;
                //        break;
                //    }
                //    return false;
                //}
                if (mListBox.mHighlightedItems.Count != 0)
                {
                    CollapseItem highlightedItem = mListBox.mHighlightedItems[0];

                    if (highlightedItem.ReferenceObject == obj)
                    {
                        MatchingList[0] = true;
                    }

                    return false;
                }
            }

            if (collectionCount > mListBox.mHighlightedItems.Count)
                return false;


            bool returnValue = true;

            for (int i = 0; i < MatchingList.Count; i++)
            {
                returnValue &= MatchingList[i];
            }

            return returnValue;
        }


        public void DeselectObject(object objectToDeselect)
        {
            mListBox.DeselectObject(objectToDeselect);
        }

		public void DisableAddingToList()
		{
			if (mAdditionButton != null)
			{

				mExtraChildrenWindows.Remove(mAdditionButton);
				RemoveWindow(mAdditionButton);
				mAdditionButton = null;
				ResizeListWindow(null);
			}
		}

		public void DisableRemovingFromList()
		{
			if (mRemovalButton != null)
			{
				mExtraChildrenWindows.Remove(mRemovalButton);
				RemoveWindow(mRemovalButton);
				mRemovalButton = null;
				ResizeListWindow(null);
			}
		}

#endif

        public Button EnableAddingToList()
        {
            bool isFixedSize = false;
            isFixedSize |= this.mAsIList != null && mAsIList.IsFixedSize;

            if (!isFixedSize)
            {
#if SILVERLIGHT

#else
                return EnableAddingToList(GetTypeOfObjectInList());
#endif
            }

            return null;
        }

        public Button EnableAddingToList(List<Type> types)
        {
#if SILVERLIGHT
            throw new NotImplementedException();
#else
            mTypeList = types;

            if (mAdditionButton == null)
            {

                mAdditionButton = new Button(mCursor);
                mAdditionButton.Text = "Add Item";

                mAdditionButton.ScaleX = 5.4f;
                AddWindow(mAdditionButton);
                mAdditionButton.Click += AddItemButtonClick;
            }


            return mAdditionButton;
#endif
        
        }
#if !SILVERLIGHT


        public Button EnableAddingToList(Type typeOfObjectInList)
        {
            mTypeOfObjectInList = typeOfObjectInList;
            mTypeList = null;

            if (mAdditionButton == null)
            {

                mAdditionButton = new Button(mCursor);
                mAdditionButton.Text = "Add Item";
                mAdditionButton.Text = "Add Item";
                mAdditionButton.ScaleX = 5.4f;
                AddWindow(mAdditionButton);
                mAdditionButton.Click += AddItemButtonClick; 
            }

            return mAdditionButton;
        }

#endif
        public Button EnableRemovingFromList()
        {
            bool isFixedSize = false;
            isFixedSize |= this.mAsIList != null && mAsIList.IsFixedSize;

            if (!isFixedSize)
            {
#if SILVERLIGHT
            throw new NotImplementedException();
#else
                return EnableRemovingFromList(true);
#endif
            }

            return null;
        }
#if !SILVERLIGHT
        public Button EnableRemovingFromList(bool showButton)
        {
            if (mRemovalButton == null)
            {
                mRemovalButton = new Button(mCursor);
            }

            if (showButton)
            {
                mRemovalButton.Text = "Remove Item";
                mRemovalButton.ScaleX = 5.4f;

                if (!mChildren.Contains(mRemovalButton))
                {
                    AddWindow(mRemovalButton);
                    mRemovalButton.Click += RemoveItemButtonClick;
                }
            }

            return mRemovalButton;
        }

#endif
        public CollapseItem GetFirstHighlightedItem()
        {
#if SILVERLIGHT
            throw new NotImplementedException();
#else
            return mListBox.GetFirstHighlightedItem();
#endif
        }
#if !SILVERLIGHT
        public object GetFirstHighlightedObject()
        {
            return mListBox.GetFirstHighlightedObject();
        }


        public object GetFirstHighlightedParentObject()
        {
            return mListBox.GetFirstHighlightedParentObject();
        }

        #region XML Docs
        /// <summary>
        /// Returns the String representation for an object.  The string returns
        /// depends on whether a string representation has been set for this object and
        /// whether the object is a FlatRedBall.Utilities.INameable.
        /// </summary>
        /// <param name="objectToGetStringFor">The object to get the string representation of.</param>
        /// <returns>The string representation of the argument.</returns>
        #endregion
        public string GetStringForObject(object objectToGetStringFor)
        {
            if (objectToGetStringFor == null)
            {
                return "<NULL>";
            }
            else
            {
                string prependedString = "";

                if (PrependIndex)
                {
                    prependedString = mAsIList.IndexOf(objectToGetStringFor).ToString() + ": ";
                }

                Type type = objectToGetStringFor.GetType();

                if (ObjectDisplayManager.sStringRepresentations.ContainsKey(type))
                {
                    return prependedString + ObjectDisplayManager.sStringRepresentations[type](objectToGetStringFor);
                }
                else if (objectToGetStringFor is Utilities.INameable)
                {
                    string name = ((Utilities.INameable)objectToGetStringFor).Name;

                    if (string.IsNullOrEmpty(name))
                        return prependedString + "<no name>";
                    else
                        return prependedString + name;
                }
                else
                {
                    return prependedString + objectToGetStringFor.ToString();
                }
            }
        }


        public void HighlightObject(object objectToHighlight, bool addToHighlighted)
        {
            mListBox.HighlightObject(objectToHighlight, addToHighlighted);
        }

        #region XML Docs
        /// <summary>
        /// Highlights the argument objectToHighlight, but does not raise the Highlighted event.
        /// </summary>
        /// <param name="objectToHighlight">The object to highlight.</param>
        /// <param name="addToHighlighted">Whether to add the argument to the highlighted items.  If 
        /// true is passed then mulitple objects can be highlighted.  If false is passed then only the 
        /// argument will be highlighted.</param>
        #endregion
        public void HighlightObjectNoCall(object objectToHighlight, bool addToHighlighted)
        {
            mListBox.HighlightObjectNoCall(objectToHighlight, addToHighlighted);
        }

#if !SILVERLIGHT
        public void HighlightItem(CollapseItem item, bool addToHighlighted)
        {
            mListBox.HighlightItem(item, addToHighlighted);
        }
#endif

        public static void SetStringRepresentationMethod(Type type, StringRepresentation stringRepresentation)
        {
			ObjectDisplayManager.SetStringRepresentationMethod(type, stringRepresentation);
        }


        public Window ShowPropertyGridForSelectedObject()
        {
#if XBOX360
            throw new NotImplementedException();
#else
            object selectedObject = mListBox.GetFirstHighlightedObject();

            

            if (selectedObject != null)
            {
                bool shouldMoveGridToTopLeft = false;
                #region If no PropertyGrids have been created so far and the user hasn't set the default location, set it to the center of the screen
                if (float.IsNaN(mNewGridX) || float.IsNaN(mNewGridY))
                {
                    shouldMoveGridToTopLeft = true;
                }
                #endregion

                if (mFloatingWindows.Count != 0)
                {
                    mNewGridX = mFloatingWindows[0].X;
                    mNewGridY = mFloatingWindows[0].Y;
                }

                Window newWindow =
                    GuiManager.ObjectDisplayManager.GetObjectDisplayerForObject(selectedObject, this) as Window;

                #region If the newly-created Window is a PropertyGrid

                if (newWindow is PropertyGrid)
                {
                    PropertyGrid newGrid = newWindow as PropertyGrid;

                    newGrid.UndoInstructions = mUndoInstructions;
                    newGrid.ContentManagerName = mContentManagerName;


                    if (newGrid is StringPropertyGrid && mOptions.Count != 0)
                    {
                        List<string> availableOptions = new List<string>();

                        foreach(object o in mOptions)
                        {
                            availableOptions.Add(o as string);
                        }

                        ((StringPropertyGrid)newGrid).SetOptions(availableOptions);

                    }

                }
                #endregion

                #region Else, if the newly-created Window is a ListDisplayWindow

                else if(newWindow is ListDisplayWindow)
                {
                    ListDisplayWindow ldw = newWindow as ListDisplayWindow;

                    ldw.UndoInstructions = mUndoInstructions;
                    ldw.ContentManagerName = ContentManagerName;
                }

                #endregion

                newWindow.HasCloseButton = true;
                newWindow.Closing += CloseChildWindow;

                if (shouldMoveGridToTopLeft)
                {
                    mNewGridX = newWindow.ScaleX;
                    mNewGridY = newWindow.ScaleY + Window.MoveBarHeight + MenuStrip.MenuStripHeight;
                }

                newWindow.X = mNewGridX + newWindow.ScaleX;
                newWindow.Y = mNewGridY + newWindow.ScaleY;

                mLastChildWindowCreated = newWindow;

                if (AfterChildWindowCreated != null)
                {
                    AfterChildWindowCreated(this);
                }

                return newWindow;
            }

            return null;
#endif
        }
        static List<object> sDisplayedObjects = new List<Object>(64);

#if !SILVERLIGHT && !WINDOWS_PHONE

        public void UpdateToList()
        {
            #region If showing a list
            if (mListShowing != null)
            {
                #region If the type of object in the list hasn't been determined, determine it here
                if (mTypeOfObjectInList == null)
                {
                    if (mAsIList != null)
                    {
                        for (int i = 0; i < mAsIList.Count; i++)
                        {
                            object o = mAsIList[i];
                            if (o != null)
                            {
                                mTypeOfObjectInList = o.GetType();
                                break;
                            }
                        }
                    }
                    else
                    {
                        foreach (object o in mListShowing)
                        {
                            if (o != null)
                            {
                                mTypeOfObjectInList = o.GetType();
                                break;
                            }
                        }
                    }
                }
                #endregion

                #region The type has been determined, so update value/Reference lists appropriately

                if (mTypeOfObjectInList != null)
                {
                    if (mTypeOfObjectInList.IsValueType || mTypeOfObjectInList == typeof(string))
                    {
                        UpdateToValueList();
                    }
                    else
                    {
                        UpdateToReferenceList();
                    }
                }

                #endregion
            }
            #endregion

            #region Else, clear the list
            else
            {
                mListBox.Clear();
            }

            #endregion

            #region Update all children PropertyGrids

            for (int i = 0; i < mFloatingWindows.Count; i++)
            {
                IWindow window = mFloatingWindows[i];
                if (window.Visible && window is PropertyGrid)
                {
                    ((PropertyGrid)window).UpdateDisplayedProperties();
                }
            }

            #endregion
        }
#endif
        public override string ToString()
        {
            if (mTypeOfObjectInList != null)
            {
                return "ListDisplayWindow Type: " + mTypeOfObjectInList.Name;
            }
            else
            {
                return "ListDisplayWindow Type: <Undetermined>";
            }
        }

        public Type GetTypeOfObjectInList()
        {
            Type typeOfObject = null;

            #region If mListShowing is not null, we can get the element type from the list
            if (mListShowing != null)
            {
                Type typeOfList = mListShowing.GetType();
                typeOfObject = GetTypeOfObjectInList(typeOfList);

            }
            #endregion

            #region Oh no!  The list is null, but not all hope is lost.  Are we part of a PropertyGrid?

            if (this.Parent != null && this.Parent is PropertyGrid)
            {
                PropertyGrid asPropertyGrid = this.Parent as PropertyGrid;

                PropertyWindowAssociation pwa = asPropertyGrid.GetPwaForWindow(this);

                string memberName = pwa.MemberName;

                // Array
                if (pwa.Type.IsArray)
                {
                    typeOfObject = pwa.Type.GetElementType();
                }
                // List
                else
                {
                    typeOfObject = pwa.Type.GetGenericArguments()[0];
                }
            }

            #endregion

            if (typeOfObject == null)
            {
                throw new InvalidOperationException("Could not find the appropriate type to create when calling the Add button");
            }
            
            return typeOfObject;
        }

        public Type GetTypeOfObjectInList(Type typeOfList)
        {
            PropertyInfo[] propertyInfos = typeOfList.GetProperties();

            foreach (PropertyInfo propertyInfo in propertyInfos)
            {
                if (propertyInfo.Name == "Item")
                {
                    return propertyInfo.PropertyType;
                }
            }

            return null;
        }


        public void SetOptions<T>(IEnumerable<T> listOfOptions)
        {
            if (typeof(T) == typeof(string))
            {
                mOptions.Clear();

                foreach (object o in listOfOptions)
                {
                    mOptions.Add(o);
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }
#endif
        #endregion

        #region Internal Methods
#if !SILVERLIGHT && !WINDOWS_PHONE && !MONODROID

        internal protected virtual void SetSelectedObjectsObjectAtIndex(int index, object value)
        {
            if (mListShowing as IList != null)
            {
                if (mAsIList != null)
                {
                    if (mAsIList.IsReadOnly)
                    {
                        GuiManager.ShowMessageBox("This list is read-only.  Editing of objects is not allowed", "List is read-only");
                        return;
                    }
                }
            }
            object[] args = { index, value };

            Type type = mListShowing.GetType();

            if (type.IsArray)
            {
                ((Array)mListShowing).SetValue(value, index);
            }
            else
            {
                type.InvokeMember("Item",
                    BindingFlags.SetProperty,
                    null, mListShowing, args);
            }

            UpdateToList();

        }
        public override void Activity(Camera camera)
        {
            base.Activity(camera);
            if (mCreationWindow.Visible)
            {
                mCreationWindow.X = this.ScreenRelativeX;
                mCreationWindow.Y = this.ScreenRelativeY + this.ScaleY - mCreationWindow.ScaleY;
            }

        }
#endif
        #endregion

        #region Private Methods

#if !SILVERLIGHT
        private CollapseItem DisplayObject(object o)
        {
            return DisplayObject(o, null);
        }

        private CollapseItem DisplayObject(object o, object parentObject)
        {
            CollapseItem newlyAddedItem = null;

            #region If considering attachments and showing IAttachables
            if (mConsiderAttachments && o is IAttachable)
            {
                IAttachable asAttachable = o as IAttachable;

                if (asAttachable.ParentAsIAttachable == null)
                {
                    newlyAddedItem = mListBox.AddItem(GetStringForObject(o), o);
                }
                else
                {
                    CollapseItem item = mListBox.GetItem(asAttachable.ParentAsIAttachable);

                    if (item == null)
                    {
                        item = DisplayObject(asAttachable.ParentAsIAttachable);

                        // If we don't do this, the CollapseItem might be added twice.
                        mTemporaryListForMembershipTest.Add(item);
//                        item = mListBox.GetItem(asAttachable.ParentAsIAttachable);
                    }

                    newlyAddedItem = item.AddItem(GetStringForObject(o), o);
                }
            }
            #endregion

            else if (mConsiderAttachments && parentObject != null)
            {
                if (parentObject == null)
                {
                    newlyAddedItem = mListBox.AddItem(GetStringForObject(o), o);
                }
                else
                {
                    CollapseItem item = mListBox.GetItem(parentObject);

                    if (item == null)
                    {
                        DisplayObject(parentObject);

                        item = mListBox.GetItem(parentObject);
                    }

                    newlyAddedItem = item.AddItem(GetStringForObject(o), o);
                }
            }
            else
            {
                newlyAddedItem = mListBox.AddItem(GetStringForObject(o), o);
            }

#if !WINDOWS_PHONE
            sDisplayedObjects.Add(o);
#endif
            return newlyAddedItem;
        }

        private bool EnumerableContains(IEnumerable enumerable, object o)
        {
            foreach (object containedObject in mListShowing)
            {
                if (o == containedObject)
                {
                    return true;
                }
            }
            return false;
        }

        private int EnumerableCount()
        {
            if (mAsIList != null)
            {
                return mAsIList.Count;
            }
            else
            {
                PropertyInfo propertyInfo = mListShowing.GetType().GetProperty("Count");

                return (int)(propertyInfo.GetValue(mListShowing, null));

            }
        }

        private int EnumerableIndexOf(object item)
        {
            int i = 0;
            foreach (object containedObject in mListShowing)
            {
                if (item == containedObject)
                {
                    return i;
                }
                i++;
            }
            return -1;
        }

        private bool EnumerableOrSubEnumerableContains(IEnumerable enumerable, object o)
        {
            foreach (object containedObject in enumerable)
            {
                if (o == containedObject)
                {
                    return true;
                }
                else if (containedObject is IEnumerable)
                {
                    if (EnumerableOrSubEnumerableContains(containedObject as IEnumerable, o))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void EnumerableRemove(object itemToRemove)
        {
            if (mAsIList != null)
            {
                mAsIList.Remove(itemToRemove);
            }
            else
            {
                MethodInfo methodInfo = mListShowing.GetType().GetMethod("Remove");

                methodInfo.Invoke(mListShowing, new object[] { itemToRemove });

            }
        }

        List<CollapseItem> mTemporaryListForMembershipTest = new List<CollapseItem>();

        private void UpdateToReferenceList()
        {
            sDisplayedObjects.Clear();

            mListBox.FillWithAllReferencedItems(sDisplayedObjects);

            bool hasIAttachables = false;

            // It's possible that a List has more than one of the same item in it.  If that's the case, then
            // there will be multiple CollapseItems showing it.
            mTemporaryListForMembershipTest.Clear();
            mTemporaryListForMembershipTest.AddRange(mListBox.Items);

            for(int i = 0; i < mListBox.Count; i++)
            {
                CollapseItem ci = mListBox[i];
                ci.FillWithAllDescendantCollapseItems(mTemporaryListForMembershipTest);
            }

            #region Add items to the ListBox if necessary
            // Loop through the mListShowing to see if there is anything in that list
            // that's not in the sDisplayedObjects


            if (mAsIList != null)
            {
                // This generates less garbage, but only possible if IList
                for (int i = 0; i < mAsIList.Count; i++)
                {
                    object o = mAsIList[i];
                    hasIAttachables = AddObjectToStaticListIfNecessary(hasIAttachables, o);
                }
            }
            else
            {
                foreach (object o in mListShowing)
                {
                    hasIAttachables = AddObjectToStaticListIfNecessary(hasIAttachables, o);

                }
            }
            #endregion

            #region Reorder list box if necessary - do this after everything else has been added

            #region Make sure the top-level ordering is correct
            
            if(mListBox.Items.Count == EnumerableCount() )
            {
                bool mHasReorderingHappened = false;
                
                foreach (object o in mListShowing)
                {
                    CollapseItem itemOfObject = mListBox.GetItem(o);
                    if (itemOfObject != null &&
                        itemOfObject.ParentItem == null &&
                        EnumerableIndexOf(o) != mListBox.Items.IndexOf(itemOfObject))
                    {
                        mListBox.Items.Remove(itemOfObject);
                        mListBox.Items.Insert(EnumerableIndexOf(o), itemOfObject);
                        mHasReorderingHappened = true;
                    }
                }

                if (mHasReorderingHappened)
                {
                    mListBox.KeepHighlightInView(GetFirstHighlightedItem());
                }
            }

            #endregion

            #endregion

            #region Add children if showing IList and showing sublists
            if (mAsIList != null && mConsiderAttachments)
            {
                for(int i = 0; i < mAsIList.Count; i++)
                {
                    object o = mAsIList[i];

                    if (o is IEnumerable)
                    {
                        UpdateSublists(o as IEnumerable, mAsIList);
                    }
                }

            }

            #endregion

            #region Remove items from the ListBox if necessary

            int count = sDisplayedObjects.Count;

            for (int i = 0; i < count; i++)
            {
                Object o = sDisplayedObjects[i];

                if (EnumerableOrSubEnumerableContains(mListShowing, o) == false)
                {
                    // o is no longer in the mListShowing, so remove the CollapseItem
                    // representing this item
                    mListBox.RemoveItemByObject(o);
                }

            }

            #endregion


        }

        private bool AddObjectToStaticListIfNecessary(bool hasIAttachables, object o)
        {
            CollapseItem itemOfObject = null;

            for (int i = 0; i < mTemporaryListForMembershipTest.Count; i++)
            {
                if (mTemporaryListForMembershipTest[i].ReferenceObject == o)
                {
                    itemOfObject = mTemporaryListForMembershipTest[i];
                    mTemporaryListForMembershipTest.RemoveAt(i);
                    break;
                }
            }

            #region There is an object in mListShowing that is not being shown by this
            if (itemOfObject == null || sDisplayedObjects.Contains(o) == false)
            {
                itemOfObject = DisplayObject(o);
            }
            #endregion

            #region The object is being displayed, so if it's an IAttachable make sure that it's attached appropriately
            else if (mConsiderAttachments && o is IAttachable)
            {
                hasIAttachables = true;

                IAttachable parent = ((IAttachable)o).ParentAsIAttachable;

                #region Object has a parent
                if (parent != null)
                {
                    CollapseItem itemOfParent = mListBox.GetItem(parent);
                    if (itemOfObject.parentItem != itemOfParent)
                    {
                        itemOfParent.AttachItemToThis(itemOfObject);
                    }
                }
                #endregion

                #region Object doesn't have a parent, make sure the CollapseItem isn't attached
                else
                {
                    if (itemOfObject.parentItem != null)
                    {
                        itemOfObject.Detach();
                    }
                }
                #endregion
            }
            #endregion

            // Update the displayed object
            itemOfObject.Text = GetStringForObject(o);
            return hasIAttachables;
        }

        private void UpdateSublists(IEnumerable iList, IEnumerable parentList)
        {
            foreach (object o in iList)
            {
				if (sDisplayedObjects.Contains(o) == false)
				{
					DisplayObject(o, iList);
				}
				else
				{
					// Let's just update the string
					CollapseItem collapseItem = mListBox.GetItem(o);

					collapseItem.Text = GetStringForObject(o);
				}

                if (o is IList)
                {
                    UpdateSublists(o as IEnumerable, iList);
                }
            }

            if (mCursor.WindowPushed != mListBox &&
                !(mCursor.WindowOver == mListBox && mCursor.PrimaryClick))
            {
                CollapseItem itemForList = mListBox.GetItem(iList);

                itemForList.ReorderToMatchList(iList);
            }
        }

        private void UpdateToValueList()
        {
            int index = 0;

            foreach (object o in mListShowing)
            {
                if (index < mListBox.Items.Count)
                {
                    mListBox.Items[index].ReferenceObject = o;
                    mListBox.Items[index].Text = GetStringForObject(o);
                }
                else
                {
                    mListBox.AddItem(GetStringForObject(o), o);
                }
                index++;
            }

            while (index < mListBox.Items.Count)
            {
                mListBox.RemoveItemAt(mListBox.Items.Count - 1);
            }
        }
#endif
        #endregion

        #endregion

        #region IObjectDisplayer<IEnumerable> Members

        public IEnumerable ObjectDisplaying
        {
            get
            {
                return ListShowing;
            }
            set
            {
                ListShowing = value;
            }
        }

        #endregion


        #region IObjectDisplayer Members

        object IObjectDisplayer.ObjectDisplayingAsObject
        {
            get
            {
                return ListShowing;
            }
            set
            {
                ListShowing = value as IEnumerable;
            }
        }

        public void UpdateToObject()
        {
#if SILVERLIGHT || WINDOWS_PHONE || MONODROID
            throw new NotImplementedException();
#else
            UpdateToList();
#endif
        }

        #endregion

    }

#if !SILVERLIGHT
    #region CreationWindow - the Window that appears when the user creates a new instance and must select the type
    internal class CreationWindow : Window
    {
        #region Fields

        internal delegate void AddDelegate(object objectToAdd);

        List<Type> mTypes;
        ComboBox typeDisplay;
        AddDelegate mAddMethod;

        #endregion

        #region Properties

        internal AddDelegate AddMethod
        {
            get { return mAddMethod; }
            set { mAddMethod = value; }
        }

        internal List<Type> Types
        {
            get { return mTypes; }
            set
            {
                mTypes = value;
                if (typeDisplay != null)
                {
                    RemoveWindow(typeDisplay);
                }
                
                ConstructTypeDisplay(GuiManager.Cursor);
                
            }

        }

        #endregion

        #region Methods

        internal CreationWindow(Cursor cursor)
            : base(cursor)
        {
            mTypes = new List<Type>();
            this.ScaleX = 10;
            this.ScaleY = 5;


            Button okButton = new Button(mCursor);
            base.AddWindow(okButton);
            okButton.Text = "OK";
            okButton.Click += CreateObject;
            okButton.ScaleX = 4;
            okButton.ScaleY = 1.6f;
            okButton.X = 5;
            okButton.Y = 8;

            Button cancelButton = new Button(mCursor);
            base.AddWindow(cancelButton);
            cancelButton.Text = "Cancel";
            cancelButton.Click += CancelCreation;
            cancelButton.ScaleX = 4;
            cancelButton.ScaleY = 1.6f;
            cancelButton.X = 15;
            cancelButton.Y = 8;

        }

        private void CreateObject(Window callingWindow)
        {
            Type selectedType = typeDisplay.SelectedObject as Type;

            Object instance = Activator.CreateInstance(selectedType);
            AddMethod(instance);
            this.Visible = false;
        }

        private void CancelCreation(Window callingWindow)
        {

            //GuiManager.RemoveWindow(this);
            this.Visible = false;

        }

        private void ConstructTypeDisplay(Cursor cursor)
        {
            typeDisplay = new ComboBox(mCursor);
            base.AddWindow(typeDisplay);
            typeDisplay.SortingStyle = ListBoxBase.Sorting.AlphabeticalIncreasing;
            typeDisplay.ScaleX = 8;
            typeDisplay.ScaleY = 2;
            typeDisplay.X = 10;
            typeDisplay.Y = 3;
            foreach(Type type in mTypes)
            {
                typeDisplay.AddItem(type.Name, type);
            }
            typeDisplay.SelectItem(0);
            

        }

        #endregion
    }
    #endregion
#endif
}
