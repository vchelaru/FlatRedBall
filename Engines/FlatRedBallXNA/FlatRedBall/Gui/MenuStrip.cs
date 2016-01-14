using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;

namespace FlatRedBall.Gui
{
    #region XML Docs
    /// <summary>
    /// UI Element used to provide compact access to functionality.  Appears in
    /// most applications as the collection of menus beginning with File, Edit, etc.
    /// </summary>
    #endregion
    public class MenuStrip : Window
    {
        #region Fields

        public const float MenuStripHeight = 3f;

        public float LeftEdge = 1;
        public float SpacingBetweenItems = 2;

        List<MenuItem> mMenus;
        ReadOnlyCollection<MenuItem> mMenusReadOnly;
        // I don't know why we have this
        // here but I've removed it because
        // it's causing compile warnings.
        //ListBox mDropDownListBoxes;

        #endregion

        #region Properties

        public ReadOnlyCollection<MenuItem> Items
        {
            get
            {
                return mMenusReadOnly;
            }
        }


        public MenuItem OpenedMenuItem
        {
            get
            {
                foreach (MenuItem menuItem in this.mMenus)
                {
                    if (menuItem.IsOpen)
                    {
                        return menuItem;
                    }
                }

                return null;
            }
        }

        #endregion

        #region Methods

        #region Constructor
        public MenuStrip(Cursor cursor)
            : base(cursor)
        {
            ScaleX = GuiManager.Camera.XEdge;
            ScaleY = MenuStripHeight/2.0f;

            X = ScaleX - .01f ;
            Y = ScaleY - .01f ;

            mMenus = new List<MenuItem>();
            mMenusReadOnly = new ReadOnlyCollection<MenuItem>(mMenus);

        }

        #endregion

        #region Public Methods

        public MenuItem AddItem(string displayText)
        {
            MenuItem menuItem = new MenuItem();
            this.AddWindow(menuItem);
            menuItem.Text = displayText;
            mMenus.Add(menuItem);

            // Make sure to update AFTER it's added
            UpdateMenuItemPositions();

            return menuItem;
        }

        public MenuItem GetItem(string displayText)
        {
            foreach (MenuItem menuItem in mMenus)
            {
                MenuItem item = menuItem.GetItem(displayText);

                if (item != null)
                    return item;
            }
            return null;
        }

        public void UpdateMenuItemPositions()
        {
            float widthOfText = LeftEdge;

            for (int i = 0; i < mMenus.Count; i++)
            {
                mMenus[i].X = widthOfText;

                widthOfText += mMenus[i].Width + SpacingBetweenItems;

            }
        }

        #endregion

        #endregion

    }
}
