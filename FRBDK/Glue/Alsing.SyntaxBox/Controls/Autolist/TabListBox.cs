// *
// * Copyright (C) 2008 Roger Alsing : http://www.RogerAlsing.com
// *
// * This library is free software; you can redistribute it and/or modify it
// * under the terms of the GNU Lesser General Public License 2.1 or later, as
// * published by the Free Software Foundation. See the included license.txt
// * or http://www.gnu.org/copyleft/lesser.html for details.
// *
// *

using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace Alsing.Windows.Forms.SyntaxBox
{
    [ToolboxItem(false)]
    public class TabListBox : ListBox
    {
        /// <summary>
        /// For public use only.
        /// </summary>
        /// <param name="keyData"></param>
        /// <returns></returns>
        protected override bool IsInputKey(Keys keyData)
        {
            return true;
        }

        /// <summary>
        /// For public use only.
        /// </summary>
        /// <param name="charCode"></param>
        /// <returns></returns>
        protected override bool IsInputChar(char charCode)
        {
            return true;
        }
    }

    /// <summary>
    /// Summary description for ListItem.
    /// </summary>
    public class ListItem : IComparable
    {
        /// <summary>
        /// The insert text of a ListItem
        /// </summary>
        public string InsertText = "";

        /// <summary>
        /// The text of a ListItem
        /// </summary>
        public string Text = "";

        /// <summary>
        /// The tooltip text that should be displayed when selecting a ListItem
        /// </summary>
        public string ToolTip = "";

        /// <summary>
        /// The type of the ListItem (the type is used as an index to choose what icon to display)
        /// </summary>
        public int Type;

        /// <summary>
        /// ListItem constructor , takes text and type as parameters
        /// </summary>
        /// <param name="text">The text that should be assigned to the ListItem</param>
        /// <param name="type">The type of the ListItem</param>
        public ListItem(string text, int type)
        {
            Text = text;
            Type = type;
            ToolTip = "";
        }

        /// <summary>
        /// ListItem constructor , takes text , type and tooltip text as parameters
        /// </summary>
        /// <param name="text">The text that should be assigned to the ListItem</param>
        /// <param name="type">The type of the ListItem</param>
        /// <param name="tooltip">The tooltip text that should be assigned to the ListItem</param>
        public ListItem(string text, int type, string tooltip)
        {
            Text = text;
            Type = type;
            ToolTip = tooltip;
        }

        /// <summary>
        /// ListItem constructor , takes text , type , tooltip text and insert text as parameters
        /// </summary>
        /// <param name="text">The text that should be assigned to the ListItem</param>
        /// <param name="type">The type of the ListItem</param>
        /// <param name="tooltip">The tooltip text that should be assigned to the ListItem</param>
        /// <param name="inserttext">The text that should be inserted into the text when this item is selected</param>
        public ListItem(string text, int type, string tooltip, string inserttext)
        {
            Text = text;
            Type = type;
            ToolTip = tooltip;
            InsertText = inserttext;
        }

        #region Implementation of IComparable

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int CompareTo(object obj)
        {
            var li = (ListItem) obj;
            return Text.CompareTo(li.Text);
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Text;
        }
    }
}