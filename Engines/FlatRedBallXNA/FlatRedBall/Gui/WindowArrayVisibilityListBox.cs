using System;
using System.Collections;
using System.Collections.Generic;
using FlatRedBall;

using FlatRedBall.Graphics;
#if FRB_MDX
using Vector2 = Microsoft.DirectX.Vector2;
#else
using Vector2 = Microsoft.Xna.Framework.Vector2;
#endif

namespace FlatRedBall.Gui
{
	/// <summary>
	/// Summary description for WindowArrayVisibilityListBox.
	/// </summary>
	public class WindowArrayVisibilityListBox : ListBox
    {
        #region Fields

        private List<Vector2> windowScls;

        #endregion

        #region Properties

        public override bool Visible
		{
			set
			{
                base.Visible = value;

				if(value == false)
				{
					foreach(CollapseItem item in mItems)
						((WindowArray)item.ReferenceObject).Visible = value;
				}
				else
				{
					foreach(CollapseItem item in mItems)
					{
                        if (this.GetHighlighted().Contains(item.Text))
						{
							((WindowArray)item.ReferenceObject).Visible = true;
						}
						else
						{
							((WindowArray)item.ReferenceObject).Visible = false;
						}
					}
				}
			}

		}
	


		#endregion

        #region Delegates
        private void SelectWindowArrayVisibility(Window callingWindow)
        {
            int i = 0;

            foreach (CollapseItem item in this.mItems)
            {
                if (this.GetHighlighted().Contains(item.Text))
                {

                    Vector2 v = windowScls[i];


                    if (float.IsNaN(v.X) == false && float.IsNaN(v.Y) == false)
                    {
                        float newScaleX = v.X;
                        float newScaleY = v.Y;

                        if (float.IsNaN(v.X))
                            newScaleX = Parent.ScaleX;
                        if (float.IsNaN(v.Y))
                            newScaleY = Parent.ScaleY;

                        Parent.SetScaleTL(newScaleX, newScaleY, true);

//                        parentWindow.X += v.X - parentWindow.ScaleX;
  //                      this.Parent.ScaleX = v.X;
                    }
                    ((WindowArray)item.ReferenceObject).Visible = true;

                }
                else
                {
                    ((WindowArray)item.ReferenceObject).Visible = false;
                }
                i++;
            }

            if (this.NumberOfVisibleElements >= Items.Count)
            {
                ScrollBarVisible = false;
            }
            else
            {
                ScrollBarVisible = true;
            }
        }

        #endregion

        #region Methods

        #region Constructor

        public WindowArrayVisibilityListBox(Cursor cursor) : 
            base(cursor)
		{
			Click += new GuiMessage(SelectWindowArrayVisibility);

			windowScls = new List<Vector2>();
        }

        #endregion

        #region Public Methods

        public void AddWindowArray(string windowName, WindowArray windowArray, float ScaleX, float ScaleY)
		{
			base.AddItem(windowName, windowArray);
			windowScls.Add( new Vector2(ScaleX, ScaleY));
		}

		public void AddWindowArray(string windowName, WindowArray windowArray)
		{
			AddWindowArray(windowName, windowArray, float.NaN, float.NaN);
		}

        public void AddWindowToCategory(IWindow windowToAdd, string category)
        {
            // If the window already exists in a category, then remove it
            // also remove the window from any WindowArrays that it belongs to
            foreach (CollapseItem collapseItem in mItems)
            {
                if (((WindowArray)collapseItem.ReferenceObject).Contains(windowToAdd))
                {
                    ((WindowArray)collapseItem.ReferenceObject).Remove(windowToAdd);
                }
            }

            ((WindowArray)GetObject(category)).Add(windowToAdd);

            SelectWindowArrayVisibility(this);
        }

        public string GetCategoryWindowBelongsTo(IWindow windowInQuestion)
        {
            foreach (CollapseItem collapseItem in mItems)
            {
                if (((WindowArray)collapseItem.ReferenceObject).Contains(windowInQuestion))
                {
                    return collapseItem.Text;
                }
            }

            return "";
        }

        public bool IsWindowInCategory(IWindow windowToCheck, string category)
        {
            WindowArray windowArray = ((WindowArray)GetObject(category));
            
            return windowArray != null && ((WindowArray)GetObject(category)).Contains(windowToCheck);
        }

        public override void HighlightItem(string itemToHighlight)
        {
            base.HighlightItem(itemToHighlight);

            SelectWindowArrayVisibility(this);
        }

        public override void RemoveWindow(IWindow windowToRemove)
        {
            base.RemoveWindow(windowToRemove);

            // also remove the window from any WindowArrays that it belongs to
            foreach (CollapseItem collapseItem in mItems)
            {
                if (((WindowArray)collapseItem.ReferenceObject).Contains(windowToRemove))
                {
                    ((WindowArray)collapseItem.ReferenceObject).Remove(windowToRemove);
                }
            }
        }

        public void SetCategoryScale(string category, float newScaleX, float newScaleY)
        {
            int index = -1;
            for (int i = 0; i < mItems.Count; i++)
            {
                if (mItems[i].Text == category)
                {
                    index = i;
                    break;
                }
            }

            

            if (index != -1)
            {
                windowScls[index] = new Vector2(newScaleX, newScaleY);
            }
        }

        #endregion

        #endregion

    }
}
