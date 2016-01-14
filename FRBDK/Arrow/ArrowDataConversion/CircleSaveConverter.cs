using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Content.Math.Geometry;
using FlatRedBall.Glue.SaveClasses;

namespace ArrowDataConversion
{
    public class CircleSaveConverter : GeneralSaveConverter
    {
        public NamedObjectSave CircleSaveToNamedObjectSave(CircleSave circle)
        {
            NamedObjectSave toReturn = new NamedObjectSave();

            toReturn.SourceType = SourceType.FlatRedBallType;
            toReturn.SourceClassType = "Circle";
            toReturn.InstanceName = circle.Name;

            AddVariablesForAllProperties(circle, toReturn);

            return toReturn;
        }
    }
}
