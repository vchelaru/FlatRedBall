using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall;


namespace FlatRedBall.Gui
{
    #region XML Docs
    /// <summary>
    /// A clickable item in a MenuStrip or item in a sub-menu.
    /// </summary>
    #endregion
    public class MenuItem : TextDisplay
    {
        #region Fields

        List<MenuItem> mMenuItems;

        bool mHighlighted;

        float mTextScale = 1;

        ListBox mChildPerishableListBox;

        ListBox mParentListBox;

        #endregion

        #region Properties

        public bool IsOpen
        {
            get { return mChildPerishableListBox != null && mChildPerishableListBox.Visible; }
        }

        internal bool Highlighted
        {
            get { return mHighlighted; }
            set { mHighlighted = value; }
        }

        public List<MenuItem> MenuItems
        {
            get { return mMenuItems; }
            set { mMenuItems = value; }
        }



        #endregion

        #region Events

        // Uses the onClick event for events which should be called
        // when the user clicks in this item.


        #endregion

        #region Delegate Methods

        void HighlightOn(Window callingWindow)
        {
            this.Highlighted = true;

            MenuStrip menuStrip = Parent as MenuStrip;

            if (menuStrip != null)
            {
                MenuItem openedItem = menuStrip.OpenedMenuItem;

                if (openedItem != null && openedItem != this)
                {
                    openedItem.Close();
                    this.OnClick(this);
                }
            }
        }

        void HighlightOff(Window callingWindow)
        {
            Highlighted = false;
        }

        void OnClick(Window callingWindow)
        {
            if (MenuItems.Count != 0)
            {
                #region Create the new ListBox and store it in mChildPerishableListBox

                mChildPerishableListBox = GuiManager.AddPerishableListBox();

                mChildPerishableListBox.SortingStyle = ListBoxBase.Sorting.None;


                mChildPerishableListBox.ScaleX = 5;
                mChildPerishableListBox.ScaleY = 5;

                mChildPerishableListBox.ScrollBarVisible = false;
                #endregion

                #region Add the MenuItems to the new ListBox

                foreach (MenuItem menuItem in MenuItems)
                {
                    CollapseItem item = mChildPerishableListBox.AddItem(menuItem.Text, menuItem);
                    menuItem.mParentListBox = this.mChildPerishableListBox;
                    item.Enabled = menuItem.Enabled;

                }

                #endregion

                #region Scale the new ListBox for the contents

                mChildPerishableListBox.SetScaleToContents(3);

                float maximumScale = GuiManager.YEdge - (MenuStrip.MenuStripHeight/2.0f);

                if (mChildPerishableListBox.ScaleY > maximumScale)
                {
                    mChildPerishableListBox.ScrollBarVisible = true;
                    mChildPerishableListBox.ScaleY = maximumScale;
                }

                #endregion

                if (this.mParentListBox != null)
                {
                    GuiManager.PersistPerishableThroughNextClick(mChildPerishableListBox);
                }

                mChildPerishableListBox.Click += ListBoxClick;

                if (this.mParentListBox != null)
                {
                    mChildPerishableListBox.X = mParentListBox.ScaleX + 
                        mParentListBox.WorldUnitX + GuiManager.UnmodifiedXEdge + mChildPerishableListBox.ScaleX;

                    int indexofHighlighted = mParentListBox.GetFirstHighlightedIndex();

                    float extraDistanceDown = mParentListBox.DistanceBetweenLines * indexofHighlighted;

                    mChildPerishableListBox.Y = 
                        -(mParentListBox.WorldUnitY - GuiManager.UnmodifiedYEdge) + mChildPerishableListBox.ScaleY - mParentListBox.ScaleY +
                        mTextScale + extraDistanceDown - mChildPerishableListBox.FirstItemDistanceFromTop/2.0f ;
                }
                else
                {
                    mChildPerishableListBox.X = X + mChildPerishableListBox.ScaleX;
                    mChildPerishableListBox.Y = Y + mChildPerishableListBox.ScaleY + mTextScale;
                }
                mChildPerishableListBox.HighlightOnRollOver = true;
            }


        }

        void ListBoxClick(Window callingWindow)
        {
            if (((ListBox)callingWindow).HighlightedCount != 0)
            {
                MenuItem selectedMenuItem =
                    ((ListBox)callingWindow).GetHighlightedObject()[0] as MenuItem;

                if (selectedMenuItem != null)
                    selectedMenuItem.OnClick();

                if (selectedMenuItem.MenuItems.Count == 0)
                {
                    GuiManager.RemoveWindow(callingWindow);
                }
            }
        }

        #endregion

        #region Methods

        #region Constructor
        public MenuItem()
            : this("")
        {
        }

        public MenuItem(string displayText)
            : base(GuiManager.Cursor)
        {
            mMenuItems = new List<MenuItem>();

            this.RollingOn += HighlightOn;
            this.RollingOff += HighlightOff;

            this.Click += OnClick;

            this.Text = displayText;

        }
        

        #endregion

        #region Public Methods

        public MenuItem AddItem(string displayText)
        {
            MenuItem menuItem = new MenuItem(displayText);
            mMenuItems.Add(menuItem);
            return menuItem;
        }

        public bool Contains(string itemText)
        {
            foreach (MenuItem menuItem in mMenuItems)
            {
                if (menuItem.Text == itemText)
                {
                    return true;
                }
            }
            return false;
        }

        public MenuItem GetItem(string displayText)
        {
            if (this.Text == displayText)
                return this;
            
            foreach(MenuItem item in mMenuItems)
            {
                MenuItem menuItem = item.GetItem(displayText);
                if (menuItem != null)
                    return menuItem;
            }

            return null;
        }

        internal override int GetNumberOfVerticesToDraw()
        {
            if (Highlighted)
            {
                return 6 + base.GetNumberOfVerticesToDraw();
            }
            else
            {
                return base.GetNumberOfVerticesToDraw();
            }
        }

        public MenuItem Insert(int index, string displayText)
        {
            MenuItem menuItem = new MenuItem(displayText);
            mMenuItems.Insert(index, menuItem);
            return menuItem;
        }


        public void Close()
        {
            if (mChildPerishableListBox != null && mChildPerishableListBox.Visible)
            {
                GuiManager.RemoveWindow(mChildPerishableListBox);
            }
        }

        public void RemoveItem(string displayText)
        {
            for (int i = 0; i < mMenuItems.Count; i++)
            {
                if (mMenuItems[i].Text == displayText)
                {
                    mMenuItems.RemoveAt(i);
                    break;
                }

            }
        }

        #endregion

        #region Private Methods

        internal override void DrawSelfAndChildren(Camera camera)
        {
            if (Highlighted)
            {
                float xPos = .5f + mWorldUnitX + Width / 2.0f;
                float yPos = mWorldUnitY;
                float highlightScaleX = .5f + this.Width / 2.0f;
                float highlightScaleY = mTextScale ;

                StaticVertices[0].Position.X = xPos - (float)highlightScaleX;
                StaticVertices[0].Position.Y = (float)(yPos - highlightScaleY);
                StaticVertices[0].TextureCoordinate.X = .0234375f;
                StaticVertices[0].TextureCoordinate.Y = .65f;

                StaticVertices[1].Position.X = xPos - (float)highlightScaleX;
                StaticVertices[1].Position.Y = (float)(yPos + highlightScaleY);
                StaticVertices[1].TextureCoordinate.X = .0234375f;
                StaticVertices[1].TextureCoordinate.Y = .65f;

                StaticVertices[2].Position.X = xPos + (float)highlightScaleX;
                StaticVertices[2].Position.Y = (float)(yPos + highlightScaleY);
                StaticVertices[2].TextureCoordinate.X = .02734375f;
                StaticVertices[2].TextureCoordinate.Y = .66f;

                StaticVertices[3] = StaticVertices[0];
                StaticVertices[4] = StaticVertices[2];

                StaticVertices[5].Position.X = xPos + (float)highlightScaleX;
                StaticVertices[5].Position.Y = (float)(yPos - highlightScaleY);
                StaticVertices[5].TextureCoordinate.X = .02734375f;
                StaticVertices[5].TextureCoordinate.Y = .66f;

                GuiManager.WriteVerts(StaticVertices);
            }

            base.DrawSelfAndChildren(camera);
        }

        #endregion

        #endregion
    }
}
