using FlatRedBall.Instructions.Reflection;
using FlatRedBall.Utilities;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// This is not built in to .NET until .net 5...
//using System.Text.Json;
// So until FRB moves to .NET 5, Path will do custom
// serialization/deserialization.
namespace FlatRedBall.Math.Paths
{
    #region Enums

    public enum SegmentType
    {
        Line,
        Arc,
        // Even though this isn't technically a path, we need to store this for serialization
        Move
    }

    public enum AngleUnit
    {
        Degrees,
        Radians
    }

    #endregion

    #region PathSegment Class
    public class PathSegment
    {
        public SegmentType SegmentType;

        public bool IsRelative;
        public float StartX;
        public float StartY;

        Vector2 Start => new Vector2(StartX, StartY);

        public float EndX;
        public float EndY;

        Vector2 End => new Vector2(EndX, EndY);

        /// <summary>
        /// The angle of the arc, which may be in degrees or radians depending on the AngleUnit value.
        /// </summary>
        public float ArcAngle;
        public Vector2 CircleCenter;

        public float CalculatedLength;

        public AngleUnit AngleUnit;

        public Vector2 PointAtLength(float lengthFromStart)
        {
            if(SegmentType == SegmentType.Line)
            {
                return Start + (End - Start).AtLength(lengthFromStart);
            }
            else if(SegmentType == SegmentType.Move)
            {
                return Start; // do we return start or end? Seems arbitrary unless there's some use case that benefits from one or the other...
            }
            else
            {
                var centerToStart = Start - CircleCenter;

                var radius = centerToStart.Length();

                var anglePerArcLength = 1 / radius;

                var angleToRotateBy = System.Math.Sign(ArcAngle) * anglePerArcLength * lengthFromStart;

                return CircleCenter + centerToStart.RotatedBy(angleToRotateBy);
            }
        }

        public override string ToString()
        {
            return $"{SegmentType}:({StartX},{StartY})=>({EndX},{EndY})";
        }
    }

    #endregion

    /// <summary>
    /// An object which can define paths using line, arc, and move to commands. Once a path is defined,
    /// it can return positions and tangents given a length. Paths are often used to define the movement
    /// of objects along a fixed path, such as moving platforms and flying enemies.
    /// </summary>
    public class Path : INameable
    {
        #region Fields/Properties

        List<PathSegment> Segments { get; set; } = new List<PathSegment>();

        float currentX;
        float currentY;

        public float TotalLength { get; private set; }
        public string Name { get; set; }

        #endregion

        #region Adding Segments

        public void MoveTo(float x, float y)
        {
            var segment = GetSegmentToAbsolutePoint( x, y, SegmentType.Move);
            segment.CalculatedLength = 0;
        }
        public void MoveToRelative(float x, float y)
        {
            var segment = GetSegmentToAbsolutePoint(currentX + x, currentY + y, SegmentType.Move);
            segment.CalculatedLength = 0;
        }

        public void LineTo(float x, float y)
        {
            var pathSegment = GetSegmentToAbsolutePoint(x, y, SegmentType.Line);
            var xDifference = pathSegment.EndX - pathSegment.StartX;
            var yDifference = pathSegment.EndY - pathSegment.StartY;

            pathSegment.CalculatedLength = (float)System.Math.Sqrt(xDifference * xDifference + yDifference * yDifference);
            Segments.Add(pathSegment);
            TotalLength += pathSegment.CalculatedLength;
        }
        public void LineToRelative(float x, float y)
        {
            var pathSegment = GetSegmentToAbsolutePoint(currentX + x, currentY + y, SegmentType.Line);
            var xDifference = pathSegment.EndX - pathSegment.StartX;
            var yDifference = pathSegment.EndY - pathSegment.StartY;

            pathSegment.CalculatedLength = (float)System.Math.Sqrt(xDifference * xDifference + yDifference * yDifference);
            Segments.Add(pathSegment);
            TotalLength += pathSegment.CalculatedLength;

        }
        PathSegment GetSegmentToAbsolutePoint(float absoluteX, float absoluteY, SegmentType segmentType)
        {
            var pathSegment = new PathSegment();

            pathSegment.SegmentType = segmentType;

            pathSegment.StartX = currentX;
            pathSegment.StartY = currentY;

            pathSegment.EndX = absoluteX;
            pathSegment.EndY = absoluteY;

            currentX = pathSegment.EndX;
            currentY = pathSegment.EndY;

            return pathSegment;
        }

        public void ShiftBy(float x, float y)
        {
            foreach(var segment in Segments)
            {
                segment.StartX += x;
                segment.EndX += x;

                segment.StartY += y;
                segment.EndY += y;

                if(segment.SegmentType == SegmentType.Arc)
                {
                    AssignArcLength(segment);

                }
            }
        }

        public void ArcTo(float endX, float endY, float signedAngleRadians)
        {
            var pathSegment = GetSegmentToAbsolutePoint(endX, endY, SegmentType.Arc);
            pathSegment.AngleUnit = AngleUnit.Radians;
            pathSegment.ArcAngle = signedAngleRadians;
            AssignArcLength(pathSegment);
            Segments.Add(pathSegment);
            TotalLength += pathSegment.CalculatedLength;

        }
        public void ArcToDegrees(float endX, float endY, float signedAngleDegrees)
        {
            var pathSegment = GetSegmentToAbsolutePoint(endX, endY, SegmentType.Arc);
            pathSegment.AngleUnit = AngleUnit.Degrees;
            pathSegment.ArcAngle = signedAngleDegrees;
            AssignArcLength(pathSegment);
            Segments.Add(pathSegment);
            TotalLength += pathSegment.CalculatedLength;
        }
        public void ArcToRelative(float endX, float endY, float signedAngleRadians)
        {
            var pathSegment = GetSegmentToAbsolutePoint(currentX + endX, currentY + endY, SegmentType.Arc);
            pathSegment.AngleUnit = AngleUnit.Radians;
            pathSegment.ArcAngle = signedAngleRadians;

            AssignArcLength(pathSegment);
            Segments.Add(pathSegment);
            TotalLength += pathSegment.CalculatedLength;

        }
        public void ArcToRelativeDegrees(float endX, float endY, float signedAngleDegrees)
        {
            var pathSegment = GetSegmentToAbsolutePoint(currentX + endX, currentY + endY, SegmentType.Arc);
            pathSegment.AngleUnit = AngleUnit.Degrees;
            pathSegment.ArcAngle = signedAngleDegrees;

            AssignArcLength(pathSegment);
            Segments.Add(pathSegment);
            TotalLength += pathSegment.CalculatedLength;
        }

        void AssignArcLength(PathSegment segment)
        {
            var first = new Vector2(segment.StartX, segment.StartY);
            var second = new Vector2(segment.EndX, segment.EndY);

            var firstToSecond = second - first;

            var angleInRadians = segment.AngleUnit == AngleUnit.Radians 
                ? segment.ArcAngle
                : MathHelper.ToRadians(segment.ArcAngle);

            var firstTangent = firstToSecond.RotatedBy(-angleInRadians / 2);
            var secondTangent = firstToSecond.RotatedBy(angleInRadians / 2);

            // normal of (x,y) is (y, -x)
            var firstNormal = new Vector2(firstTangent.Y, -firstTangent.X);
            var secondNormal = new Vector2(secondTangent.Y, -secondTangent.X);


            // from here:
            // https://stackoverflow.com/questions/4543506/algorithm-for-intersection-of-2-lines
            // Update, this had a bug and I coulnd't figure it out because I don't understand
            // the math well enough to debug it. Moving to a different solution:

            var firstSegment = new Geometry.Segment(segment.StartX, segment.StartY,
                segment.StartX + firstNormal.X, segment.StartY + firstNormal.Y);

            var secondSegment = new Geometry.Segment(segment.EndX, segment.EndY,
                segment.EndX + secondNormal.X, segment.EndY + secondNormal.Y);

            var intersection = FindIntersection(firstSegment, secondSegment);


            //void GetABC(Geometry.Segment segmentInner, out double A, out double B, out double C)
            //{

            //    // using A = y2-y1; B = x1-x2; C = Ax1+By1
            //    A = segmentInner.Point2.Y - segmentInner.Point1.Y;
            //    B = segmentInner.Point1.X - segmentInner.Point2.X;
            //    C = A * segmentInner.Point1.X + B * segmentInner.Point1.Y;

            //}

            //GetABC(firstSegment, out double A1, out double B1, out double C1);
            //GetABC(secondSegment, out double A2, out double B2, out double C2);

            //var delta = A1 * B2 - A2 * B1;

            float radius;

            if (intersection == null || intersection == first || intersection == second)
            {
                radius = (first - second).Length()/2.0f;
                segment.CircleCenter = (first + second) / 2.0f;
            }
            else
            {
                segment.CircleCenter = intersection.Value;

                radius = (first - segment.CircleCenter).Length();
            }

            segment.CalculatedLength = System.Math.Abs(radius * angleInRadians);

        }

        //  Returns Point of intersection if do intersect otherwise default Point (null)
        static Vector2? FindIntersection(Geometry.Segment lineA, Geometry.Segment lineB, double tolerance = 0.001)
        {
            double x1 = lineA.Point1.X, y1 = lineA.Point1.Y;
            double x2 = lineA.Point2.X, y2 = lineA.Point2.Y;

            double x3 = lineB.Point1.X, y3 = lineB.Point1.Y;
            double x4 = lineB.Point2.X, y4 = lineB.Point2.Y;

            // equations of the form x = c (two vertical lines)
            if (System.Math.Abs(x1 - x2) < tolerance && System.Math.Abs(x3 - x4) < tolerance && System.Math.Abs(x1 - x3) < tolerance)
            {
                return null;
            }

            //equations of the form y=c (two horizontal lines)
            if (System.Math.Abs(y1 - y2) < tolerance && System.Math.Abs(y3 - y4) < tolerance && System.Math.Abs(y1 - y3) < tolerance)
            {
                return null;
            }

            //equations of the form x=c (two vertical parallel lines)
            if (System.Math.Abs(x1 - x2) < tolerance && System.Math.Abs(x3 - x4) < tolerance)
            {
                return null;
            }

            //equations of the form y=c (two horizontal parallel lines)
            if (System.Math.Abs(y1 - y2) < tolerance && System.Math.Abs(y3 - y4) < tolerance)
            {
                return null;
            }

            //general equation of line is y = mx + c where m is the slope
            //assume equation of line 1 as y1 = m1x1 + c1 
            //=> -m1x1 + y1 = c1 ----(1)
            //assume equation of line 2 as y2 = m2x2 + c2
            //=> -m2x2 + y2 = c2 -----(2)
            //if line 1 and 2 intersect then x1=x2=x & y1=y2=y where (x,y) is the intersection point
            //so we will get below two equations 
            //-m1x + y = c1 --------(3)
            //-m2x + y = c2 --------(4)

            double x, y;

            //lineA is vertical x1 = x2
            //slope will be infinity
            //so lets derive another solution
            if (System.Math.Abs(x1 - x2) < tolerance)
            {
                //compute slope of line 2 (m2) and c2
                double m2 = (y4 - y3) / (x4 - x3);
                double c2 = -m2 * x3 + y3;

                //equation of vertical line is x = c
                //if line 1 and 2 intersect then x1=c1=x
                //subsitute x=x1 in (4) => -m2x1 + y = c2
                // => y = c2 + m2x1 
                x = x1;
                y = c2 + m2 * x1;
            }
            //lineB is vertical x3 = x4
            //slope will be infinity
            //so lets derive another solution
            else if (System.Math.Abs(x3 - x4) < tolerance)
            {
                //compute slope of line 1 (m1) and c2
                double m1 = (y2 - y1) / (x2 - x1);
                double c1 = -m1 * x1 + y1;

                //equation of vertical line is x = c
                //if line 1 and 2 intersect then x3=c3=x
                //subsitute x=x3 in (3) => -m1x3 + y = c1
                // => y = c1 + m1x3 
                x = x3;
                y = c1 + m1 * x3;
            }
            //lineA & lineB are not vertical 
            //(could be horizontal we can handle it with slope = 0)
            else
            {
                //compute slope of line 1 (m1) and c2
                double m1 = (y2 - y1) / (x2 - x1);
                double c1 = -m1 * x1 + y1;

                //compute slope of line 2 (m2) and c2
                double m2 = (y4 - y3) / (x4 - x3);
                double c2 = -m2 * x3 + y3;

                //solving equations (3) & (4) => x = (c1-c2)/(m2-m1)
                //plugging x value in equation (4) => y = c2 + m2 * x
                x = (c1 - c2) / (m2 - m1);
                y = c2 + m2 * x;

                //verify by plugging intersection point (x, y)
                //in orginal equations (1) & (2) to see if they intersect
                //otherwise x,y values will not be finite and will fail this check
                if (!(System.Math.Abs(-m1 * x + y - c1) < tolerance
                    && System.Math.Abs(-m2 * x + y - c2) < tolerance))
                {
                    //return default (no intersection)
                    return null;
                }
            }

            ////x,y can intersect outside the line segment since line is infinitely long
            ////so finally check if x, y is within both the line segments
            //if (IsInsideLine(lineA, x, y) &&
            //    IsInsideLine(lineB, x, y))
            //{
            return new Vector2((float)x, (float)y);
            //}

        }
        #endregion

        /// <summary>
        /// Clears all contained segments, resets the TotalLength to 0, and sets the current X and Y values for the next segment to 0.
        /// </summary>
        public void Clear()
        {
            Segments.Clear();
            TotalLength = 0;
            currentX = 0;
            currentY = 0;
        }

        public void FlipHorizontally(float xToFlipAbout = 0)
        {
            foreach(var segment in this.Segments)
            {
                segment.ArcAngle = segment.ArcAngle * -1;

                var centerOffset = segment.CircleCenter.X - xToFlipAbout;
                segment.CircleCenter.X = xToFlipAbout - centerOffset;

                var endXOffset = segment.EndX - xToFlipAbout;
                segment.EndX = xToFlipAbout - endXOffset;

                var startXOffset = segment.StartX - xToFlipAbout;
                segment.StartX = xToFlipAbout - startXOffset;
            }
        }

        #region Point, Length, Tangent at XXXX

        public Vector2 PointAtLength(float length)
        {
            var lengthSoFar = 0f;
            var spilloverLength = 0f;
            PathSegment segmentToUse = null;

            if (length >= TotalLength && Segments.Count > 0)
            {
                var segment = Segments[Segments.Count - 1];
                return segment.PointAtLength(segment.CalculatedLength);
            }
            else
            {
                for(int i = 0; i < Segments.Count; i++)
                {
                    var segmentLength = Segments[i].CalculatedLength;
                    if(lengthSoFar + segmentLength > length)
                    {
                        segmentToUse = Segments[i];
                        spilloverLength = length - lengthSoFar;
                        break;
                    }
                    else
                    {
                        lengthSoFar += segmentLength;
                    }
                }

                if(segmentToUse != null)
                {
                    return segmentToUse.PointAtLength(spilloverLength);
                }
                else
                {
                    return Vector2.Zero; 
                }
            }

        }

        public Vector2 PointAtSegmentIndex(int index)
        {
            var segment = Segments[index];
            return new Vector2(segment.StartX, segment.StartY);
        }

        public float LengthAtSegmentIndex(int index)
        {
            var lengthSoFar = 0f;

            for(int i = 0; i < index; i++)
            {
                lengthSoFar += Segments[i].CalculatedLength;
            }

            return lengthSoFar;
        }

        /// <summary>
        /// Returns the tangent unit vector at the argument length. If the tangent cannot be calculated, then a unit vector
        /// pointing to the right will be returned (a unit vector at angle 0).
        /// </summary>
        /// <remarks>
        /// The tangent is calculated by looking slightly in front and slightly behind the path. Therefore, values near sharp corners,
        /// or paths with wavy paths may return incorrect values.
        /// </remarks>
        /// <param name="length">The location along the Path.</param>
        /// <param name="epsilon">The amount of distance to look in front and behind. Values which are very wavy may require a smaller epsilon. Paths with very large values may require a larger epsilon.</param>
        /// <returns>A tangent unit vector, or a unit vector pointing to the right if a tangent cannot be calculated.</returns>
        public Vector2 TangentAtLength(float length, float epsilon = 0.1f)
        {
            var startValue = length - epsilon;
            var endValue = length + epsilon;

            if (startValue < 0) startValue = 0;
            if (endValue > TotalLength) endValue = TotalLength;

            var tangent = PointAtLength(endValue) - PointAtLength(startValue);

            return tangent.NormalizedOrRight();
        }

        #endregion

        #region JSON Serialization

        public void FromJson(string serializedSegments)
        {
            if(!string.IsNullOrWhiteSpace(serializedSegments))
            {
                var deserialized = ParseToSegmentList(serializedSegments);
                Clear();
                foreach (var item in deserialized)
                {
                    if (item.SegmentType == SegmentType.Line)
                    {
                        LineToRelative(item.EndX, item.EndY);
                    }
                    else if (item.SegmentType == SegmentType.Arc)
                    {
                        var angle = item.ArcAngle;
                        if(item.AngleUnit == AngleUnit.Degrees)
                        {
                            angle = Microsoft.Xna.Framework.MathHelper.ToRadians(angle);
                        }
                        ArcToRelative(item.EndX, item.EndY, angle);
                    }
                    else if(item.SegmentType == SegmentType.Move)
                    {
                        MoveToRelative(item.EndX, item.EndY);
                    }
                    else
                    {
                        // Unknown segment type...
                    }
                }
            }
        }

        private List<PathSegment> ParseToSegmentList(string serializedSegments)
        {
            var toReturn = new List<PathSegment>();
            // example:
            /*
             * [
             *  {
             *      "SegmentType":0,
             *      "IsRelative":false,
             *      "StartX":0.0,
             *      "StartY":0.0,
             *      "EndX":1.0,
             *      "EndY":96.0,
             *      "ArcAngle":0.0,
             *      "CircleCenter":"0, 0",
             *      "CalculatedLength":0.0,
             *      "AngleUnit":0
             *  },
             *  {
             *      "SegmentType":1,"IsRelative":false,"StartX":0.0,"StartY":0.0,"EndX":60.0,"EndY":0.0,"ArcAngle":-180.0,"CircleCenter":"0, 0",
             *      "CalculatedLength":0.0,"AngleUnit":0
             *  }
             *  ,
             *  {"SegmentType":0,"IsRelative":false,"StartX":0.0,"StartY":0.0,"EndX":0.0,"EndY":-96.0,"ArcAngle":0.0,"CircleCenter":"0, 0","CalculatedLength":0.0,"AngleUnit":0},{"SegmentType":1,"IsRelative":false,"StartX":0.0,"StartY":0.0,"EndX":-58.0,"EndY":0.0,"ArcAngle":-180.0,"CircleCenter":"0, 0","CalculatedLength":0.0,"AngleUnit":0}]
             * 
             */
            // remove [ and ] at the dstart and end:
            serializedSegments = serializedSegments.Substring(1, serializedSegments.Length - 2);

            // split on }
            var segments = serializedSegments.Split('}')
                .Select(item =>
                {
                    if(item?.Length > 0)
                    {
                        return item.Substring(1).Replace("{", "");
                    }
                    else
                    {
                        return item;
                    }
                })
                .Where(item => !string.IsNullOrEmpty(item))
                .ToArray();


            foreach(var segment in segments)
            {
                var pathSegment = new PathSegment();

                var splitNameValues = segment.Split('\"')
                    .Where(item => !string.IsNullOrEmpty(item) && item != "," && item != ":")
                    .Select(item =>
                    {
                        if(item.StartsWith(":"))
                        {
                            item = item.Substring(1);
                        }
                        if(item.EndsWith(","))
                        {
                            item = item.Substring(0, item.Length - 1);
                        }
                        return item;
                    })
                    .ToArray();

                for(int i = 0; i < splitNameValues.Length; i+= 2)
                {
                    AssignValueOnSegment(pathSegment, splitNameValues[i], splitNameValues[i + 1]);
                }

                toReturn.Add(pathSegment);
            }

            

            return toReturn;
        }

        private void AssignValueOnSegment(PathSegment segment, string propertyName, string v2)
        {
            object parsedValue = null;
            var apply = false;
            switch(propertyName)
            {
                case nameof(PathSegment.SegmentType):
                    parsedValue = (SegmentType)int.Parse(v2);
                    apply = true;
                    break;
                case nameof(PathSegment.EndX):
                case nameof(PathSegment.EndY):
                case nameof(PathSegment.ArcAngle):
                    parsedValue = float.Parse(v2, System.Globalization.CultureInfo.InvariantCulture);
                    apply = true;
                    break;
                case nameof(PathSegment.AngleUnit):
                    parsedValue = (AngleUnit)int.Parse(v2);
                    apply = true;
                    break;
            }

            if(apply)
            {
                LateBinder.SetValueStatic(segment, propertyName, parsedValue);
            }
        }

        #endregion

    }
}
