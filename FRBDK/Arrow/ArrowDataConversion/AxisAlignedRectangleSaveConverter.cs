using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Content.Math.Geometry;
using FlatRedBall.Glue.SaveClasses;

namespace ArrowDataConversion
{
    public class AxisAlignedRectangleSaveConverter : GeneralSaveConverter
    {
        public NamedObjectSave RectangleSaveToNamedObjectSave(AxisAlignedRectangleSave rectangle)
        {
            NamedObjectSave toReturn = new NamedObjectSave();

            toReturn.SourceType = SourceType.FlatRedBallType;
            toReturn.SourceClassType = "AxisAlignedRectangle";
            toReturn.InstanceName = rectangle.Name;

            AddVariablesForAllProperties(rectangle, toReturn);

            return toReturn;
        }
    }
}
