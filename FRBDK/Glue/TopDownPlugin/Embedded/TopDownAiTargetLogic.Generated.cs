$GLUE_VERSIONS$


using FlatRedBall;
using FlatRedBall.AI.Pathfinding;
using FlatRedBall.Math;
using FlatRedBall.Math.Geometry;
using Microsoft.Xna.Framework;

namespace $NAMESPACE$.TopDown
{
    public class TopDownAiTargetLogic
    {
        public float? CloserThan { get; set; }
        public float? FartherThan { get; set; }
        public bool LineOfSight { get; set; }
        public float? AngleToTarget { get; set; }

        Line firstLineOfSightLine = new Line();
        Line secondLineOfSightLine = new Line();

        public Vector3 GetTargetLocation<T>(TopDownAiInput<T> aiInput, Vector3 focusPoint) where T : PositionedObject, TopDown.ITopDownEntity
        {
            if (AngleToTarget != null)
            {
                return GetAngledTargetLocation(aiInput, focusPoint);
            }
            else if (LineOfSight)
            {
                return GetAnyAngleLineOfSightTargetLocation(aiInput, focusPoint);
            }
            else if (FartherThan != null)
            {
                return DoCloserThanFurtherThanLogic(aiInput, focusPoint);
            }
            else
            {
                return focusPoint;
            }
        }

        private Vector3 GetAngledTargetLocation<T>(TopDownAiInput<T> aiInput, Vector3 focusPoint) where T : PositionedObject, ITopDownEntity
        {
            var effectiveTarget = focusPoint;

            float maxDistance = float.PositiveInfinity;
            float minDistance = 0;

#if ShapeManagerCollideAgainstClosest

            if (LineOfSight)
            {
                var angleToVector = Vector3ExtensionMethods.FromAngle(AngleToTarget.Value);
                // make it really long!
                angleToVector *= 100_000;

                var firstLinePosition = effectiveTarget;
                firstLinePosition.Y -= aiInput.CollisionWidth / 2;
                MathFunctions.RotatePointAroundPoint(effectiveTarget, ref firstLinePosition, AngleToTarget.Value);

                var secondLinePosition = effectiveTarget;
                secondLinePosition.Y += aiInput.CollisionWidth / 2;
                MathFunctions.RotatePointAroundPoint(effectiveTarget, ref secondLinePosition, AngleToTarget.Value);

                firstLineOfSightLine.Position = firstLinePosition;
                firstLineOfSightLine.RelativePoint1 = new Point3D();
                firstLineOfSightLine.RelativePoint2 = new Point3D(angleToVector);

                secondLineOfSightLine.Position = secondLinePosition;
                secondLineOfSightLine.RelativePoint1 = new Point3D();
                secondLineOfSightLine.RelativePoint2 = new Point3D(angleToVector);

                Vector3? closestFirstCollisionPoint = null;
                float closestFirstLength = 0;
                Vector3? closestSecondCollisionPoint = null;
                float closestSecondLength = 0;

                for (int i = 0; i < aiInput.EnvironmentCollision.Count; i++)
                {
                    var collision = aiInput.EnvironmentCollision[i];

                    var firstCollided = collision.CollideAgainstClosest(firstLineOfSightLine);

                    if (firstCollided)
                    {
                        if (closestFirstCollisionPoint == null)
                        {
                            closestFirstCollisionPoint = firstLineOfSightLine.LastCollisionPoint.ToVector3();
                            closestFirstLength = (closestFirstCollisionPoint.Value - firstLinePosition).Length();
                        }
                        else
                        {
                            var newCollisionPoint = firstLineOfSightLine.LastCollisionPoint.ToVector3();

                            var newLength = (newCollisionPoint - firstLinePosition).Length();

                            if (newLength < closestFirstLength)
                            {
                                closestFirstCollisionPoint = firstLineOfSightLine.LastCollisionPoint.ToVector3();
                                closestFirstLength = (closestFirstCollisionPoint.Value - firstLinePosition).Length();
                            }
                        }
                    }

                    var secondCollided = collision.CollideAgainstClosest(secondLineOfSightLine);

                    if (secondCollided)
                    {
                        if (closestSecondCollisionPoint == null)
                        {
                            closestSecondCollisionPoint = secondLineOfSightLine.LastCollisionPoint.ToVector3();
                            closestSecondLength = (closestSecondCollisionPoint.Value - secondLinePosition).Length();
                        }
                        else
                        {
                            var newCollisionPoint = secondLineOfSightLine.LastCollisionPoint.ToVector3();

                            var newLength = (newCollisionPoint - secondLinePosition).Length();

                            if (newLength < closestSecondLength)
                            {
                                closestSecondCollisionPoint = secondLineOfSightLine.LastCollisionPoint.ToVector3();
                                closestSecondLength = (closestSecondCollisionPoint.Value - secondLinePosition).Length();
                            }
                        }
                    }

                }

                if (closestFirstCollisionPoint != null && closestSecondCollisionPoint == null)
                {
                    maxDistance = closestFirstLength;
                }
                else if (closestFirstCollisionPoint == null && closestSecondCollisionPoint != null)
                {
                    maxDistance = closestSecondLength;
                }
                else if (closestFirstCollisionPoint != null && closestSecondCollisionPoint != null)
                {
                    if (closestFirstLength < closestSecondLength)
                    {
                        maxDistance = closestFirstLength;
                    }
                    else
                    {
                        maxDistance = closestSecondLength;
                    }
                }
            }
#endif

            if (CloserThan != null)
            {
                if (CloserThan < maxDistance)
                {
                    maxDistance = CloserThan.Value;
                }
            }

            if (FartherThan != null)
            {
                // min distance can't be greater than max distance
                minDistance = FartherThan.Value;
                if (minDistance > maxDistance)
                {
                    minDistance = maxDistance;
                }
            }


            // we really can't use positive infinity for math here, so we'll trim it to something reasonable like 100k
            if (float.IsPositiveInfinity(maxDistance))
            {
                maxDistance = 100_000f;
            }

            // now create a segment to see the closest point:
            var directionVector = Vector3ExtensionMethods.FromAngle(AngleToTarget.Value);

            var segment = new Segment(effectiveTarget + directionVector * minDistance, effectiveTarget + directionVector * maxDistance);

            float tileSize = GetTileSize(aiInput);

            if (segment.DistanceTo(aiInput.Owner.Position) < tileSize)
            {
                // close enough, walk towards the player
                return DoCloserThanFurtherThanLogic(aiInput, focusPoint, moveTowardsFocusIfInRange: true);
            }
            else
            {
                var closestPoint = segment.ClosestPointTo(aiInput.Owner.Position);

                return closestPoint.ToVector3();
            }
        }

        private Vector3 GetAnyAngleLineOfSightTargetLocation<T>(TopDownAiInput<T> aiInput, Vector3 focusPoint) where T : PositionedObject, ITopDownEntity
        {
            var effectiveTarget = focusPoint;

            var lineOfSightPoly = aiInput.GetLineOfSightPathFindingPolygon(aiInput.Owner.Position, effectiveTarget);

            // default to true, set to false if any collision
            var hasLineOfSight = true;
            for (int i = 0; i < aiInput.EnvironmentCollision.Count; i++)
            {
                var environment = aiInput.EnvironmentCollision[i];

                if (environment.CollideAgainst(lineOfSightPoly))
                {
                    hasLineOfSight = false;
                    break;
                }
            }

            // if doesn't have line of sight, then just move towards the target
            if (!hasLineOfSight)
            {
                return effectiveTarget;
            }
            else
            {
                return DoCloserThanFurtherThanLogic(aiInput, focusPoint);
            }
        }

        private Vector3 DoCloserThanFurtherThanLogic<T>(TopDownAiInput<T> aiInput, Vector3 focusPoint, bool moveTowardsFocusIfInRange = false) where T : PositionedObject, ITopDownEntity
        {
            var effectiveTarget = focusPoint;

            // if it does have line of sight, try to move away from the target. Set a target that is far away. If the enemy breaks line of sight it will resume 
            // to the !hasLineOfSight logic later. This is the simplest approach even though it may result in some back-and-forth movement. Eventually we could refine
            // this but who cares for now.
            var vectorToTarget = effectiveTarget - aiInput.Owner.Position;

            var directionFromTargetToOwner = vectorToTarget.NormalizedOrRight();

            var lengthToTarget = vectorToTarget.Length();
            if (CloserThan < lengthToTarget)
            {
                // player is too far, move closer!
                return effectiveTarget;
            }
            else if (FartherThan > lengthToTarget)
            {
                // player is too close, move far away
                // but how far? 
                // Less than one tile may result in the player standing still
                // Exactly one tile may result in the same tile being selected due to floating point issues
                // More than one tile may select a location in the wall
                // So let's select slightly more than one tile:

                float tileSize = GetTileSize(aiInput);

                var targetPosition = aiInput.Owner.Position + (tileSize * 1.1f) * directionFromTargetToOwner * -1;
                return targetPosition;
            }
            else
            {
                if (moveTowardsFocusIfInRange)
                {
                    return focusPoint;
                }
                else
                {
                    // we're good where we are!
                    return aiInput.Owner.Position;
                }
            }
        }

        private static float GetTileSize<T>(TopDownAiInput<T> aiInput) where T : PositionedObject, ITopDownEntity
        {
            float tileSize = 16; // default to 16, but let's look to objects for more accuracy:
            if (aiInput.EnvironmentCollision.Count > 0)
            {
                tileSize = aiInput.EnvironmentCollision[0].GridSize;
            }
            else if (aiInput.NodeNetwork is TileNodeNetwork tileNodeNetwork)
            {
                tileSize = tileNodeNetwork.GridSpacing;
            }

            return tileSize;
        }
    }
}
