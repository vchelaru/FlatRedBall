$GLUE_VERSIONS$
using FlatRedBall;
using FlatRedBall.Input;
using FlatRedBall.Math.Geometry;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace $NAMESPACE$.TopDown
{
    public class TopDownAiInput<T> : IInputDevice where T : PositionedObject, TopDown.ITopDownEntity
    {
        public static float RequiredDistanceForNextTarget = 16;

        bool isPathVisible;
        public bool IsPathVisible
        {
            get => isPathVisible;
            set
            {
                if (value != isPathVisible)
                {
                    isPathVisible = value;
                    UpdateLines();
                }
            }
        }


        List<Line> ownedLines = new List<Line>();

        public bool RemoveTargetOnReaching
        {
            get; set;
        }

        public bool ShouldRemoveLastTarget { get; set; } = true;

        public bool StopOnTarget
        {
            get; set;
        } = true;

        public bool IsActive
        {
            get; set;
        } = true;

        public event Action<T> TargetReached;

        #region Internal Classes
        class Values2DInput : I2DInput
        {
            public float X { get; set; }

            public float Y { get; set; }

            public float XVelocity => throw new NotImplementedException();

            public float YVelocity => throw new NotImplementedException();

            public float Magnitude => (float)System.Math.Sqrt( (X*X) + (Y*Y));
        }

        #endregion

        #region IInputDevice Properties

        Values2DInput values2DInput = new Values2DInput();
        public virtual I2DInput Default2DInput => values2DInput;

#if HasIRepeatPressableInput || REFERENCES_FRB_SOURCE

        public virtual IRepeatPressableInput DefaultUpPressable => throw new NotImplementedException();
        public virtual IRepeatPressableInput DefaultDownPressable => throw new NotImplementedException();
        public virtual IRepeatPressableInput DefaultLeftPressable => throw new NotImplementedException();
        public virtual IRepeatPressableInput DefaultRightPressable => throw new NotImplementedException();
#else

        public virtual IPressableInput DefaultUpPressable => throw new NotImplementedException();
        public virtual IPressableInput DefaultDownPressable => throw new NotImplementedException();
        public virtual IPressableInput DefaultLeftPressable => throw new NotImplementedException();
        public virtual IPressableInput DefaultRightPressable => throw new NotImplementedException();
#endif

        public virtual I1DInput DefaultHorizontalInput => throw new NotImplementedException();

        public virtual I1DInput DefaultVerticalInput => throw new NotImplementedException();

        public virtual IPressableInput DefaultPrimaryActionInput => throw new NotImplementedException();
        public virtual IPressableInput DefaultSecondaryActionInput => throw new NotImplementedException();

        public virtual IPressableInput DefaultConfirmInput => throw new NotImplementedException();
        public virtual IPressableInput DefaultCancelInput => throw new NotImplementedException();

        public virtual IPressableInput DefaultJoinInput => throw new NotImplementedException();

        public virtual IPressableInput DefaultPauseInput => throw new NotImplementedException();

        public virtual IPressableInput DefaultBackInput => throw new NotImplementedException();
#endregion

        public Vector3? NextImmediateTarget { get; set; }

        public PositionedObject FollowingTarget { get; set; }
        public Vector3? TargetPosition { get; set; }

        public FlatRedBall.AI.Pathfinding.NodeNetwork NodeNetwork { get; set; }

        public List<Vector3> Path { get; private set; } = new List<Vector3>();

        public T Owner { get; set; }

        public TopDownAiInput(T owner)
        {
            this.Owner = owner;
        }

        public void Activity()
        {
            UpdatePath();

            DoTargetFollowingActivity();
        }

        public void UpdatePath()
        {
            Vector3? effectivePosition = FollowingTarget?.Position ?? TargetPosition;
            if (effectivePosition != null && NodeNetwork != null)
            {
                var pathfindingTarget = effectivePosition.Value;

                var lineToTarget = pathfindingTarget - Owner.Position;
                //var perpendicular = new Vector3(-lineToTarget.Y, lineToTarget.X, 0);
                //if (perpendicular.Length() != 0)
                //{
                //    perpendicular.Normalize();
                //    var distanceFromTarget = lineToTarget.Length();

                //    const float distanceToPerpendicularLengthRatio = 1 / 2f;

                //    pathfindingTarget = target.Position + perpendicular * perpendicularLengthRatio * distanceToPerpendicularLengthRatio * distanceFromTarget;

                //}

                var hadDirectPath = Path.Count == 1 && isUsingLineOfSightPathfinding;
                var hasDirectPathNow = false;
                if (hadDirectPath)
                {
                    var fromVector = Owner.Position;
                    var toVector = pathfindingTarget;

                    GetLineOfSightPathFindingPolygon(fromVector, toVector);

                    var hasClearPath = true;

                    for (int i = 0; i < EnvironmentCollision.Count; i++)
                    {
                        var collision = EnvironmentCollision[i];
                        if (collision.CollideAgainst(lineOfSightPathFindingPolygon))
                        {
                            hasClearPath = false;
                        }
                    }

                    if (hasClearPath)
                    {
                        Path[0] = pathfindingTarget;
                    }

                    hasDirectPathNow = hasClearPath;
                }

                if (!hasDirectPathNow)
                {

                    var points = NodeNetwork.GetPositionPath(ref Owner.Position, ref pathfindingTarget);
                    Path.Clear();
                    //var points = path.Select(item => item.Position).ToList();

                    // So the enemy doesn't stop on the nearest node without attacking the player:
                    points.Add(pathfindingTarget);

                    
                    if (isUsingLineOfSightPathfinding)
                    {
                        // We will only cut line of sight points if we have more than one
                        // point. Look at the next pointn to see if there's a direct path. If 
                        // so, cut this one.
                        while (points.Count > 1)
                        {
                            var fromVector = Owner.Position;
                            var toVector = points[1];

                            GetLineOfSightPathFindingPolygon(fromVector, toVector);

                            var hasClearPath = true;

                            for (int i = 0; i < EnvironmentCollision.Count; i++)
                            {
                                var collision = EnvironmentCollision[i];
                                if (collision.CollideAgainst(lineOfSightPathFindingPolygon))
                                {
                                    hasClearPath = false;
                                }
                            }

                            if (hasClearPath)
                            {
                                points.RemoveAt(0);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }

                    Path.AddRange(points);
                }
                NextImmediateTarget = Path.FirstOrDefault();
            }
            else
            {
                Path.Clear();
            }

            UpdateLines();
        }

        private void UpdateLines()
        {
            if (IsPathVisible)
            {
                while (ownedLines.Count < Path.Count)
                {
                    var line = new Line();
                    line.Color = new Color(1f, 1f, 0);

                    line.Visible = true;
                    ownedLines.Add(line);
                }
                while (Path.Count < ownedLines.Count)
                {
                    var lastLine = ownedLines[ownedLines.Count - 1];
                    lastLine.Visible = false;
                    ownedLines.RemoveAt(ownedLines.Count - 1);
                }

                for (int i = 0; i < Path.Count; i++)
                {
                    if (i == 0)
                    {
                        ownedLines[0].SetFromAbsoluteEndpoints(Owner.Position, Path[0]);
                    }
                    else
                    {
                        ownedLines[i].SetFromAbsoluteEndpoints(Path[i - 1], Path[i]);
                    }
                }
            }
            else
            {
                while (ownedLines.Count > 0)
                {
                    ownedLines[0].Visible = false;
                    ownedLines.RemoveAt(0);
                }
            }
        }

        public FlatRedBall.Math.Geometry.Polygon GetLineOfSightPathFindingPolygon(Vector3 fromVector, Vector3 toVector)
        {
            var length = (toVector - Owner.Position).Length();
            lineOfSightPathFindingPolygon.SetPoint(0, length / 2.0f, CollisionWidth / 2);
            lineOfSightPathFindingPolygon.SetPoint(1, length / 2.0f, -CollisionWidth / 2);
            lineOfSightPathFindingPolygon.SetPoint(2, -length / 2.0f, -CollisionWidth / 2);
            lineOfSightPathFindingPolygon.SetPoint(3, -length / 2.0f, CollisionWidth / 2);
            lineOfSightPathFindingPolygon.SetPoint(4, length / 2.0f, CollisionWidth / 2);

            lineOfSightPathFindingPolygon.X = (toVector.X + fromVector.X) / 2.0f;
            lineOfSightPathFindingPolygon.Y = (toVector.Y + fromVector.Y) / 2.0f;

            var angle = (float)System.Math.Atan2(toVector.Y - fromVector.Y, toVector.X - fromVector.X);
            lineOfSightPathFindingPolygon.RotationZ = angle;

            return lineOfSightPathFindingPolygon;
        }

    public void DoTargetFollowingActivity()
    {
        values2DInput.X = 0;
        values2DInput.Y = 0;

        if (NextImmediateTarget != null && Owner?.CurrentMovement != null && IsActive)
        {
            void RemoveFromPath()
            {
                TargetReached?.Invoke(Owner);
                if (Path.Count > 0)
                {
                    NextImmediateTarget = Path[0];
                    Path.RemoveAt(0);

                    if (Path.Count > 0)
                    {
                        // do it again
                        DoTargetFollowingActivity();
                    }
                }
                else
                {
                    NextImmediateTarget = null;
                }
            }
            var targetX = NextImmediateTarget.Value.X;
            var targetY = NextImmediateTarget.Value.Y;

            var xDiff = targetX - Owner.Position.X;
            var yDiff = targetY - Owner.Position.Y;

            var isCloseToFindNextTarget =
                Math.Abs(xDiff) < RequiredDistanceForNextTarget && Math.Abs(yDiff) < RequiredDistanceForNextTarget;

            var shouldRemove = isCloseToFindNextTarget &&
                RemoveTargetOnReaching &&
                Path.Count > 1;

            if (shouldRemove)
            {
                RemoveFromPath();
            }
            else if (xDiff != 0 || yDiff != 0)
            {
                bool shouldMoveFullSpeed = false;
                if (StopOnTarget || Path.Count == 1)
                {
                    var currentMovementLength = Owner.Velocity.Length();
                    var currentRatioOfMax = currentMovementLength / Owner.CurrentMovement.MaxSpeed;

                    var currentTimeToSlowDown = currentRatioOfMax * Owner.CurrentMovement.DecelerationTime;
                    var maxSpeed = Owner.CurrentMovement.MaxSpeed;
                    var maxAccelerationValue = -maxSpeed / Owner.CurrentMovement.DecelerationTime;

                    //// create the temporary vectors:
                    // Not sure where but there's an off-by-1 error somewhere, so account for it by subtracting one frame.
                    var position = new Vector3((float)(2 * currentMovementLength *
                         +FlatRedBallServices.Game.TargetElapsedTime.TotalSeconds), 0, 0);
                    var velocity = new Vector3(currentMovementLength, 0, 0);
                    var acceleration = new Vector3(maxAccelerationValue, 0, 0);

                    var positionAfterTime = FlatRedBall.Math.MathFunctions.GetPositionAfterTime(
                      ref position,
                      ref velocity,
                      ref acceleration,
                      currentTimeToSlowDown);

                    var lengthToSlow = Math.Abs(positionAfterTime.X);
                    shouldMoveFullSpeed = (xDiff * xDiff) + (yDiff * yDiff) > lengthToSlow * lengthToSlow;

                    if (!shouldMoveFullSpeed && ShouldRemoveLastTarget)
                    {
                        RemoveFromPath();
                    }
                }
                else if (Path.Count > 0)
                {
                    shouldMoveFullSpeed = true;
                }

                if (shouldMoveFullSpeed)
                {

                    var angle = (float)System.Math.Atan2(yDiff, xDiff);

                    values2DInput.X = (float)Math.Cos(angle);
                    values2DInput.Y = (float)Math.Sin(angle);
                }
            }
        }
    }
    public float CollisionWidth { get; private set; }
        public List<FlatRedBall.TileCollisions.TileShapeCollection> EnvironmentCollision { get; private set; }
        bool isUsingLineOfSightPathfinding = false;
        static FlatRedBall.Math.Geometry.Polygon lineOfSightPathFindingPolygon;
        public void SetLineOfSightPathfinding(float collisionWidth, List<FlatRedBall.TileCollisions.TileShapeCollection> collision)
        {
            this.CollisionWidth = collisionWidth;
            this.EnvironmentCollision = collision;
            isUsingLineOfSightPathfinding = true;
            if (lineOfSightPathFindingPolygon == null)
            {
                lineOfSightPathFindingPolygon = FlatRedBall.Math.Geometry.Polygon.CreateEquilateral(4, 1, 0);
            }
        }

    }


}


