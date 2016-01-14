using System;
using System.Collections.Generic;
using System.Text;
#if FRB_XNA || SILVERLIGHT || WINDOWS_PHONE
using Microsoft.Xna.Framework.Graphics;
#endif

namespace FlatRedBall.Gui
{
    #region XML Docs
    /// <summary>
    /// Delegate for actions to be taken when the user clicks on an icon.
    /// </summary>
    /// <param name="collapseItem">The CollapseItem containing the icon that was clicked.</param>
    /// <param name="listBoxBase">The ListBoxBase containing the CollapseItem that contains the ListBoxIcon.</param>
    /// <param name="listBoxIcon">The ListboxIcon that was clicked.</param>
    #endregion
    public delegate void ListBoxFunction(CollapseItem collapseItem, ListBoxBase listBoxBase, ListBoxIcon listBoxIcon);

    public class ListBoxIcon
    {
        #region Fields

        public float Top = 0;
        public float Bottom = 1;
        public float Left = 0;
        public float Right = 1;
        
        public string Name;


        #endregion

        #region Properties

        public bool Enabled
        {
            get;
            set;
        }

        public float ScaleX
        {
            get;
            set;
        }

        public float ScaleY
        {
            get;
            set;
        }

        public Texture2D Texture
        {
            get;
            set;
        }

        #endregion

        #region Events

        public event ListBoxFunction IconClick;

        #endregion

        #region Methods

        public ListBoxIcon(Texture2D texture, string name)
        {
            ScaleX = .7f;
            ScaleY = .7f;

            this.Name = name;
            this.Texture = texture;
            Enabled = true;
        }

        public ListBoxIcon(float top, float bottom, float left, float right, string name)
        {
            ScaleX = .7f;
            ScaleY = .7f;

            Top = top;
            Bottom = bottom;
            Left = left;
            Right = right;
            Name = name;

            IconClick = null;
            Enabled = true;
        }

        internal void RaiseIconClick(CollapseItem collapseItem, ListBoxBase listBoxBase)
        {
            if (IconClick != null)
            {
                IconClick(collapseItem, listBoxBase, this);
            }
        }

        #endregion

    }
}
