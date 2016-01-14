using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Math.Geometry;
using FlatRedBall.Instructions;

namespace EditorObjects.Undo
{
    public class PolygonPropertyComparer : PropertyComparer<Polygon>
    {
        public override InstructionList GetChangedMemberInstructions(Polygon objectToWatch)
        {
            InstructionList listToReturn = base.GetChangedMemberInstructions(objectToWatch);

            Polygon polygonToCompareAgainst = mObjectsWatching[objectToWatch];


            #region See if points differ
            bool pointsDiffer = false;

            for(int i = 0; i < objectToWatch.Points.Count; i++)
            {
                if (objectToWatch.Points[i] != polygonToCompareAgainst.Points[i])
                {
                    pointsDiffer = true;
                    break;
                }                
            }

            if (pointsDiffer)
            {
                Point[] oldPoints = new Point[polygonToCompareAgainst.Points.Count];

                
                polygonToCompareAgainst.Points.CopyTo(oldPoints, 0);


                listToReturn.Add(
                    new Instruction<Polygon, Point[]>(
                        objectToWatch, "Points", oldPoints, 0));

                polygonToCompareAgainst.Points = objectToWatch.Points;
            }
            #endregion

            return listToReturn;
        }

        public override void UpdateWatchedObject(Polygon objectToUpdate)
        {
            base.UpdateWatchedObject(objectToUpdate);

            Polygon instanceToUseAsClone = mObjectsWatching[objectToUpdate];

            instanceToUseAsClone.Points = objectToUpdate.Points;
        }
    }
}
