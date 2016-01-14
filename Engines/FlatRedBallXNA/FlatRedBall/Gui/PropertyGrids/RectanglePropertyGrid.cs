using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

#if FRB_XNA
using Rectangle = Microsoft.Xna.Framework.Rectangle;
#else
using Rectangle = System.Drawing.Rectangle;
#endif

namespace FlatRedBall.Gui.PropertyGrids
{
    public class RectanglePropertyGrid : StructReferencePropertyGrid<Rectangle>
    {
        public override bool IsWindowOrChildrenReceivingInput
        {
            get
            {
                return base.IsWindowOrChildrenReceivingInput;
            }
        }

        public RectanglePropertyGrid(Cursor cursor, PropertyGrid propertyGridOfObject, string nameOfProperty) :
            base(cursor, propertyGridOfObject, nameOfProperty)
        {
            ExcludeMembers();

        }

        public RectanglePropertyGrid(Cursor cursor, ListDisplayWindow windowOfObject, int indexOfObject)
            : base(cursor,
                windowOfObject, indexOfObject) 
        {
            ExcludeMembers();
        }



        private void ExcludeMembers()
        {
            ExcludeMember("Location");
            ExcludeMember("Size");
            ExcludeMember("IsEmpty");
            ExcludeMember("Empty");
        }

        
    }
}
