using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Gui;
using FlatRedBall.Content.Model.Helpers;

namespace EditorObjects.Gui
{
    public class CustomModelPropertyGrid : PropertyGrid<CustomModel>
    {
        public CustomModelPropertyGrid(Cursor cursor)
            : base(cursor)
        {


        }


    }
}
