using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Gui;
using Microsoft.Xna.Framework;

namespace PolygonEditorXna.IO
{
    public class AdvancedShapeCollectionLoadingWindow : Window
    {
        Vector3Display offsetWindow;

        public Vector3 Offset
        {
            get { return offsetWindow.Vector3Value; }
        }

        public AdvancedShapeCollectionLoadingWindow(Cursor cursor, MultiButtonMessageBox mbmb, TypesToLoad typesToLoad) : base(cursor)
        {
            this.AddWindow(mbmb);

            this.ScaleX = mbmb.ScaleX + 12.5f;
            mbmb.DrawBorders = false;
            mbmb.X = mbmb.ScaleX;

            mbmb.RemoveButton(mbmb.GetButton("Advanced >>"));
            // Maybe one day we want to allow the user to go back to the 
            // basic view?  It's a pain, so I won't do that now

            mbmb.Y = mbmb.ScaleY;
            this.ScaleY = mbmb.ScaleY + 1;


            //Button cancelButton = mbmb.GetButton("Cancel");
            //mbmb.RemoveButton(cancelButton);
            mbmb.HasMoveBar = false;
            mbmb.HasCloseButton = false;
            this.HasMoveBar = true;

            //mbmb.AddButton(cancelButton);

            TextDisplay textDisplay = new TextDisplay(cursor);
            textDisplay.Text = "Offset";
            this.AddWindow(textDisplay);
            textDisplay.X = mbmb.ScaleX * 2;
            textDisplay.Y = 1;

            offsetWindow = new Vector3Display(cursor);
            this.AddWindow(offsetWindow);
            offsetWindow.X = mbmb.ScaleX * 2 + offsetWindow.ScaleX ;
            offsetWindow.Y = offsetWindow.ScaleY + 2;

            mbmb.Closing += new GuiMessage(CloseThis);
            this.Name = "";

            PropertyGrid<TypesToLoad> propertyGrid = new PropertyGrid<TypesToLoad>(cursor);
            propertyGrid.ObjectDisplaying = typesToLoad;
            propertyGrid.HasMoveBar = false;
            propertyGrid.HasCloseButton = false;

            this.AddWindow(propertyGrid);
            propertyGrid.X = mbmb.ScaleX * 2 + propertyGrid.ScaleX - 1; // subtract 1 because we're not going to show the frames
            propertyGrid.Y = propertyGrid.ScaleY + 2 + offsetWindow.ScaleY * 2 + .5f;

            propertyGrid.DrawBorders = false;

        }

        void CloseThis(Window callingWindow)
        {
            GuiManager.RemoveWindow(this);
        }
    }
}
