using System;
using System.Collections.Generic;
using System.Text;

using FlatRedBall.Math.Geometry;
using FlatRedBall.Graphics;

using Vector2 = Microsoft.Xna.Framework.Vector2;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Ray = Microsoft.Xna.Framework.Ray;
using Matrix = Microsoft.Xna.Framework.Matrix;
using Quaternion = Microsoft.Xna.Framework.Quaternion;
using MathHelper = Microsoft.Xna.Framework.MathHelper;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

using Plane = Microsoft.Xna.Framework.Plane;
using FlatRedBall.Graphics.Animation;

namespace FlatRedBall.Math
{
    #region Enums
    public enum CoordinateSystem
    {
        RightHanded,
        LeftHanded
    }

    public enum Axis
    {
        X,
        Y,
        Z
    }
    #endregion    

    public static class MathFunctions
    {
        #region Fields
        public static Vector3 ForwardVector3 = Vector3.Forward;
        public static Plane ZPlane = new Plane(new Vector3(0, 0, 1), 0);

        static Vector3 sMethodCallVector3;

        public const float MatrixOrthonormalEpsilon = .001f;

        #endregion

        #region Methods

        public static void AbsoluteToWindow(float x, float y, float z, ref int screenX, ref int screenY, Camera camera)
        {
            AbsoluteToWindow(x, y, z, ref screenX, ref screenY, camera, true);
        }

        public static void AbsoluteToWindow(float x, float y, float z, ref int screenX, ref int screenY, Camera camera, bool rotate)
        {
            AbsoluteToWindow(x, y, z, ref screenX, ref screenY, camera, rotate, camera.XEdge, camera.YEdge);
        }

        public static void AbsoluteToWindow(float x, float y, float z, ref int screenX, ref int screenY, Camera camera, bool rotate, float xEdge, float yEdge)
        {
            if (camera.RotationMatrix != Matrix.Identity && rotate)
            {
                Matrix inverseMatrix = Matrix.Invert(camera.RotationMatrix);

                Vector3 vectorToTransform = new Vector3(x, y, z);

                vectorToTransform -= camera.Position;

                TransformVector(ref vectorToTransform, ref inverseMatrix);

                vectorToTransform += camera.Position;


                x = vectorToTransform.X;
                y = vectorToTransform.Y;
                z = vectorToTransform.Z;
            }

            float xRatioOver = (x - camera.X) / camera.RelativeXEdgeAt(z);
            xRatioOver += 1;
            xRatioOver /= 2.0f;

            float yRatioOver = (y - camera.Y) / camera.RelativeYEdgeAt(z);
            yRatioOver -= 1;
            yRatioOver /= -2.0f;


            // get the top left
            //            topLeftPoint = new System.Drawing.Point(form.ClientRectangle.Left, form.ClientRectangle.Top);

            screenX = (int)(xRatioOver * camera.DestinationRectangle.Width);
            screenY = (int)(yRatioOver * camera.DestinationRectangle.Height);
        }

        /// <summary>
        /// Determines the shortest absolute difference between two angles. For example, AngleToAngle(PI/4, -PI/4) will return -PI/2.
        /// </summary>
        /// <remarks>
        /// This method will never return a value with absolute value greater than PI.  It will return 
        /// either a positive or negative value, keeping all values between positive and negative PI.
        /// </remarks>
        /// <param name="angle1">Starting angle in radians.</param>
        /// <param name="angle2">Ending angle in radians.</param>
        /// <returns>The number of radians between the two angles.</returns>
        public static float AngleToAngle(float angle1, float angle2)
        {
            float angle = angle2 - angle1;

            if (System.Math.Abs(angle) > System.Math.PI)
                angle = (float)(angle - 2 * System.Math.Sign(angle) * System.Math.PI);

            return angle;
        }

        public static double AngleToAngle(double angle1, double angle2)
        {
            double angle = angle2 - angle1;

            if (System.Math.Abs(angle) > System.Math.PI)
                angle = (angle - 2 * System.Math.Sign(angle) * System.Math.PI);

            return angle;
        }

        #region XML Docs
        /// <summary>
        /// Extracts the RotationX, RotationY, and RotationZ values out of a 
        /// </summary>
        /// <param name="matrixToExtractFrom">The matrix to get the rotation values out of.</param>
        /// <param name="rotationX">The RotationX of the Matrix.</param>
        /// <param name="rotationY">The RotationY of the Matrix.</param>
        /// <param name="rotationZ">The RotationZ of the Matrix.</param>
        #endregion
        public static void ExtractRotationValuesFromMatrix(Matrix matrixToExtractFrom, ref float rotationX, ref float rotationY, ref float rotationZ)
        {
            rotationX = (float)System.Math.Atan2(matrixToExtractFrom.M23, matrixToExtractFrom.M33);
            rotationZ = (float)System.Math.Atan2(matrixToExtractFrom.M12, matrixToExtractFrom.M11);
            rotationY = -(float)System.Math.Asin(matrixToExtractFrom.M13);

            if (rotationX < 0)
                rotationX += (float)System.Math.PI * 2;
            if (rotationY < 0)
                rotationY += (float)System.Math.PI * 2;
            if (rotationZ < 0)
                rotationZ += (float)System.Math.PI * 2;
        }

        static int frameToFrameInteger;

        #region XML Docs
        /// <summary>
        /// Determines the shortest absolute difference between two frames.
        /// </summary>
        /// <remarks>
        /// This method will consider moving forward and backward, as well as cycling from the end
        /// to the beginning of an AnimationChain.
        /// </remarks>
        /// <param name="ac">The animationChain to use when determining the distance.</param>
        /// <param name="frame1">The first frame.</param>
        /// <param name="frame2">the second frame.</param>
        /// <returns></returns>
        #endregion
        public static int FrameToFrame(AnimationChain ac, int frame1, int frame2)
        {
            frameToFrameInteger = frame2 - frame1;

            if (frameToFrameInteger > ac.Count / 2.0)
                frameToFrameInteger -= ac.Count;
            else if (frameToFrameInteger < -ac.Count / 2.0)
                frameToFrameInteger += ac.Count;

            return frameToFrameInteger;

        }

        public static float GetAspectRatioForSameSizeAtResolution(float resolution)
        {
            double yAt600 = System.Math.Sin(System.Math.PI / 8.0);
            double xAt600 = System.Math.Cos(System.Math.PI / 8.0);

            double desiredYAt600 = yAt600 * (double)resolution / 600.0;
            float desiredAngle = (float)System.Math.Atan2(desiredYAt600, xAt600);

            return 2 * desiredAngle; 
        }

        public static List<Microsoft.Xna.Framework.Point> GetGridLine(int x0, int y0, int x1, int y1)
        {
            var toReturn = new List<Microsoft.Xna.Framework.Point>();
            int dy = y1 - y0;
            int dx = x1 - x0;
            int stepx, stepy;

            if (dy < 0) { dy = -dy; stepy = -1; } else { stepy = 1; }
            if (dx < 0) { dx = -dx; stepx = -1; } else { stepx = 1; }
            dy <<= 1;                                                  // dy is now 2*dy
            dx <<= 1;                                                  // dx is now 2*dx

            toReturn.Add(new Microsoft.Xna.Framework.Point
            {
                X = x0,
                Y = y0,
            });

            if (dx > dy)
            {
                int fraction = dy - (dx >> 1);                         // same as 2*dy - dx
                while (x0 != x1)
                {
                    if (fraction >= 0)
                    {
                        y0 += stepy;
                        fraction -= dx;                                // same as fraction -= 2*dx
                    }
                    x0 += stepx;
                    fraction += dy;                                    // same as fraction -= 2*dy

                    //if (x0 > -1 && y0 > -1 &&
                    //    x0 < mNumberOfXTiles && y0 < mNumberOfYTiles)
                    {
                        toReturn.Add(new Microsoft.Xna.Framework.Point { X = x0, Y = y0 });
                    }
                }
            }
            else
            {
                int fraction = dx - (dy >> 1);
                while (y0 != y1)
                {
                    if (fraction >= 0)
                    {
                        x0 += stepx;
                        fraction -= dy;
                    }
                    y0 += stepy;
                    fraction += dx;

                    //if (x0 > -1 && y0 > -1 &&
                    //    x0 < mNumberOfXTiles && y0 < mNumberOfYTiles)
                    {
                        toReturn.Add(new Microsoft.Xna.Framework.Point { X = x0, Y = y0 });
                    }
                }
            }

            return toReturn;
        }

        public static Point GetPointInCircle(float radius)
        {
            double zeroToOne = FlatRedBallServices.Random.NextDouble();
            double zeroToTwoPi = FlatRedBallServices.Random.NextDouble() * 2 * System.Math.PI;
            double distanceFromCenter = 
                System.Math.Sqrt(zeroToOne) * radius; // Fix distribution and scale to [0, RealRadius]

            Point pointToReturn = new Point(
                System.Math.Cos(zeroToTwoPi) * distanceFromCenter,
                System.Math.Sin(zeroToTwoPi) * distanceFromCenter);

            return pointToReturn;
        }

        public static Point GetPointInTriangle(Point point0, Point point1, Point point2)
        {

            Point vector0 = point1 - point0;
            Point vector1 = point2 - point0;

            double valueX = FlatRedBallServices.Random.NextDouble();
            double valueY = FlatRedBallServices.Random.NextDouble();

            if (valueX + valueY > 1)
            {
                double tempX = 1 - valueY;
                valueY = 1 - valueX;
                valueX = tempX;
            }

            return point0 + (vector0 * valueX) + (vector1 * valueY)  ;
        }



        #region XML Docs
        /// <summary>
        /// Returns a random point on the surface of a unit sphere.
        /// </summary>
        /// <param name="random">Reference to a Random instance.</param>
        /// <param name="xPos">The resulting X value.</param>
        /// <param name="yPos">The resulting Y value.</param>
        /// <param name="zPos">The resulting Z value.</param>
        #endregion
        public static void GetPointOnUnitSphere(Random random, out float xPos, out float yPos, out float zPos)
        {
            // Marsaglia (1972)
            double x1 = (-1 + 2 * random.NextDouble());
            double x2 = (-1 + 2 * random.NextDouble());

            while (x1*x1 + x2*x2 >= 1)
            {
                x1 = (-1 + 2 * random.NextDouble());
                x2 = (-1 + 2 * random.NextDouble());
            }

            xPos = (float)(2 * x1 * System.Math.Sqrt(1 - x1 * x1 - x2 * x2));
            yPos = (float)(2 * x2 * System.Math.Sqrt(1 - x1 * x1 - x2 * x2));
            zPos = (float)(1 - 2 * (x1 * x1 + x2 * x2));
        }

        public static Vector3 GetPositionAfterTime(ref Vector3 startPosition, ref Vector3 velocity, ref Vector3 acceleration, double time)
        {
            double secondDifferenceSquaredDividedByTwo = (time * time) / 2;

            Vector3 returnValue = 
                startPosition + 
                velocity * (float)time + 
                acceleration * (float)secondDifferenceSquaredDividedByTwo;

            return returnValue;

        }

        public static Ray GetRay(int pixelX, int pixelY, float distanceAlongForwardVector, Camera camera, LayerCameraSettings layerCameraSettings)
        {
            if (layerCameraSettings == null)
            {
                return GetRay(pixelX, pixelY, distanceAlongForwardVector, camera);
            }
            else
            {
                Ray ray = new Ray();

                if (layerCameraSettings.Orthogonal)
                {
                    ray.Direction = camera.RotationMatrix.Forward;

                    float centerPixelX = 0;
                    float centerPixelY = 0;

                    float halfPixelWidth = 0;
                    float halfPixelHeight = 0;

                    float halfUnitWidth = 0;
                    float halfUnitHeight = 0;

                    //6/1/2011
                    // We used to
                    // set our center
                    // and halfwidth values
                    // according to the Layer
                    // if using a layer that has
                    // an overriding destination rect,
                    // but this seemed to make the clicking
                    // inaccurate on 2D screens at Wahoo. Taking
                    // it out fixes all problesm.  Perhaps this isn't
                    // needed?
                    //if (layerCameraSettings.TopDestination < 0)
                    //{
                    centerPixelX = camera.DestinationRectangle.Left + camera.DestinationRectangle.Width / 2.0f;
                    centerPixelY = camera.DestinationRectangle.Top + camera.DestinationRectangle.Height / 2.0f;



                    // Update May 28, 2012
                    // It looks like this code
                    // used to assume that the ortho
                    // width/height matched the DestinationRectangle.
                    // That may not be true so we should account for that.
                    halfPixelWidth = camera.DestinationRectangle.Width / 2;
                    halfPixelHeight = camera.DestinationRectangle.Height / 2;

                    bool handledZooming = false;
                    // May 31, 2012
                    // We only want to
                    // zoom if the Layer is
                    // fullscreen.  Otherwise
                    // it is probably only part
                    // of the screen
                    if ((layerCameraSettings.LeftDestination == camera.LeftDestination || layerCameraSettings.LeftDestination == -1) &&
                        (layerCameraSettings.RightDestination == camera.RightDestination || layerCameraSettings.RightDestination == -1) &&
                        (layerCameraSettings.TopDestination == camera.TopDestination || layerCameraSettings.TopDestination == -1) &&
                        (layerCameraSettings.BottomDestination == camera.BottomDestination || layerCameraSettings.BottomDestination == -1) &&
                        layerCameraSettings.OrthogonalWidth > 0 && layerCameraSettings.OrthogonalHeight > 0)
                    {
                        // Handle zooming
                        halfUnitWidth = layerCameraSettings.OrthogonalWidth / 2.0f;
                        halfUnitHeight = layerCameraSettings.OrthogonalHeight / 2.0f;
                        handledZooming = true;
                    }
                    else
                    {
                        halfUnitWidth = halfPixelWidth;
                        halfUnitHeight = halfPixelHeight;

                    }
                    //}
                    //else
                    //{
                    //    centerPixelX = (layerCameraSettings.LeftDestination + layerCameraSettings.RightDestination) / 2.0f;
                    //    centerPixelY = (layerCameraSettings.TopDestination + layerCameraSettings.BottomDestination) / 2.0f;

                    //    halfWidth = (layerCameraSettings.RightDestination - layerCameraSettings.LeftDestination) / 2.0f;
                    //    halfHeight = (layerCameraSettings.BottomDestination - layerCameraSettings.TopDestination) / 2.0f;
                    //}




                    float normalizedX = (pixelX - centerPixelX) / halfPixelWidth;
                    float normalizedY = -(pixelY - centerPixelY) / halfPixelHeight;


                    // This may not account for the up vector...need to consider that!
                    ray.Position += normalizedX * camera.RotationMatrix.Right * halfUnitWidth;
                    ray.Position += normalizedY * camera.RotationMatrix.Up * halfUnitHeight;

                    if (layerCameraSettings.RightDestination != layerCameraSettings.LeftDestination &&
                        layerCameraSettings.TopDestination != layerCameraSettings.BottomDestination && 
                        !handledZooming)
                    {
                        float pixelToOrthoRatioX = layerCameraSettings.OrthogonalWidth /
                            (layerCameraSettings.RightDestination - layerCameraSettings.LeftDestination);

                        ray.Position.X *= pixelToOrthoRatioX;

                        float pixelToOrthoRatioY = layerCameraSettings.OrthogonalHeight /
                            (layerCameraSettings.BottomDestination - layerCameraSettings.TopDestination);

                        ray.Position.Y *= pixelToOrthoRatioY;


                    }

                }
                else
                {
                    pixelX -= camera.DestinationRectangle.Left;
                    pixelY -= camera.DestinationRectangle.Top;


                    int halfWidth = camera.DestinationRectangle.Width / 2;
                    int halfHeight = camera.DestinationRectangle.Height / 2;

                    float normalizedX = (pixelX - halfWidth) / (float)halfWidth;
                    float normalizedY = -(pixelY - halfHeight) / (float)halfHeight;

                    float absoluteZ = camera.Position.Z + distanceAlongForwardVector * MathFunctions.ForwardVector3.Z;

                    float centerToEdgeX =
                        SpriteManager.Camera.RelativeXEdgeAt(absoluteZ,
                        layerCameraSettings.FieldOfView, camera.AspectRatio, layerCameraSettings.Orthogonal, layerCameraSettings.OrthogonalWidth);
                    float centerToEdgeY =
                        SpriteManager.Camera.RelativeYEdgeAt(absoluteZ,
                        layerCameraSettings.FieldOfView, camera.AspectRatio, layerCameraSettings.Orthogonal, layerCameraSettings.OrthogonalHeight);


                    Vector3 untranslatedRelative = new Vector3(0, 0, distanceAlongForwardVector * MathFunctions.ForwardVector3.Z);

                    untranslatedRelative.X += centerToEdgeX * normalizedX;
                    untranslatedRelative.Y += centerToEdgeY * normalizedY;

                    Matrix rotationMatrix = camera.RotationMatrix;

                    MathFunctions.TransformVector(ref untranslatedRelative, ref rotationMatrix);

                    ray.Direction = untranslatedRelative;
                }


                ray.Position += camera.Position;


                if (layerCameraSettings.ExtraRotationZ != 0)
                {
#if FRB_XNA
                    Matrix rotationMatrix = Matrix.CreateFromAxisAngle(camera.RotationMatrix.Backward, layerCameraSettings.ExtraRotationZ);

                    ray.Position -= camera.Position;

                    ray.Position = Vector3.Transform(ray.Position, rotationMatrix);
                    ray.Direction = Vector3.Transform(ray.Direction, rotationMatrix);
                    ray.Position += camera.Position;
#endif
                }


                return ray;

            }

        }


        public static Ray GetRay(int pixelX, int pixelY, float distanceAlongForwardVector, Camera camera)
        {
#if DEBUG
            if (camera == null)
            {
                throw new ArgumentException("The camera must not be null when calling GetRay", "camera");

            }
#endif
            Ray ray = new Ray();

            pixelX -= camera.DestinationRectangle.Left;
            pixelY -= camera.DestinationRectangle.Top;


            ray.Position = camera.Position;
            
            int halfWidth = camera.DestinationRectangle.Width / 2;
            int halfHeight = camera.DestinationRectangle.Height / 2;

            float normalizedX = (pixelX - halfWidth) / (float)halfWidth;
            float normalizedY = -(pixelY - halfHeight) / (float)halfHeight;

            if (camera.Orthogonal)
            {

                float halfOrthoWidth = camera.OrthogonalWidth / 2.0f;
                float halfOrthoHeight = camera.OrthogonalHeight / 2.0f;

                Matrix rotationMatrix = Matrix.Invert(camera.GetLookAtMatrix(true));

                ray.Direction = rotationMatrix.Forward;
                // This may not account for the up vector...need to consider that!
                ray.Position += normalizedX * rotationMatrix.Right * halfOrthoWidth;
                ray.Position += normalizedY * rotationMatrix.Up * halfOrthoHeight;
            }
            else
            {

                float centerToEdgeX = camera.RelativeXEdgeAt(camera.Position.Z + distanceAlongForwardVector * MathFunctions.ForwardVector3.Z);
                float centerToEdgeY = camera.RelativeYEdgeAt(camera.Position.Z + distanceAlongForwardVector * MathFunctions.ForwardVector3.Z);

                Vector3 untranslatedRelative = new Vector3(0, 0, distanceAlongForwardVector * MathFunctions.ForwardVector3.Z);

                untranslatedRelative.X += centerToEdgeX * normalizedX;
                untranslatedRelative.Y += centerToEdgeY * normalizedY;

                // We used to use the RotationMatrix here, but
                // that didn't take into account the up vector that's
                // applied when rendering.  This fixes some ray casting
                // issues; however, this may need to be migrated above to
                // the 2D case as well.
                //Matrix rotationMatrix = Matrix.Invert(camera.GetLookAtMatrix(false));

                Vector3 cameraTarget = camera.RotationMatrix.Forward;

                Matrix rotationMatrix;
                var position = Vector3.Zero;
                Matrix.CreateLookAt(
                    ref position, // Position of the camera eye
                    ref cameraTarget,  // Point that the eye is looking at
                    ref camera.UpVector,
                    out rotationMatrix);

                rotationMatrix = Matrix.Invert(rotationMatrix);

                MathFunctions.TransformVector(ref untranslatedRelative, ref rotationMatrix);

                ray.Direction = untranslatedRelative;

                ray.Direction.Normalize();
            }
            return ray;

        }

        internal static Ray GetRay(float xAt100Units, float yAt100Units, Camera camera)
        {
            Ray rayToReturn;

            Vector3 cursorVector = new Vector3();

            #region Get the vector from the camera out and store it in cursorVector

            if (camera.Orthogonal == false)
            {
                cursorVector.X = xAt100Units;// + tipXOffset);
                cursorVector.Y = yAt100Units;// + tipYOffset);
                
                cursorVector.Z = FlatRedBall.Math.MathFunctions.ForwardVector3.Z * 100.0f;

                rayToReturn.Position = camera.Position;
            }
            else
            {// when in ortho, we want to "move" the sprite to the center of the screen and run an 
                float horizontalPercentage = 0;
                float verticalPercentage = 0;

                horizontalPercentage = (xAt100Units) / camera.XEdge;
                verticalPercentage = (yAt100Units) / camera.YEdge;


                float xDistanceFromCenter = horizontalPercentage * camera.OrthogonalWidth / 2.0f;
                float yDistanceFromCenter = verticalPercentage * camera.OrthogonalHeight / 2.0f;

                cursorVector.X = 0;
                cursorVector.Y = 0;
                cursorVector.Z = 1;

                rayToReturn.Position = new Vector3(camera.X + xDistanceFromCenter, camera.Y + yDistanceFromCenter, 0);
            }
            #endregion


            Matrix cameraRotationMatrix = camera.RotationMatrix;

            MathFunctions.TransformVector(ref cursorVector, ref cameraRotationMatrix);

            cursorVector.Normalize();

            rayToReturn.Direction = cursorVector;

            return rayToReturn;
        }


        internal static bool IsOn3D<T>(T objectToTest, bool relativeToCamera, Ray mouseRay,
            Camera camera,
            out Vector3 intersectionPoint) where T : IPositionable, IRotatable, IReadOnlyScalable
        {
            intersectionPoint = Vector3.Zero;

            if (objectToTest == null)
                return false;

            mouseRay.Direction.Normalize();

            #region Do a 3D test since the object might be rotated on all axes

                Vector3 topEdge = new Vector3(1, 0, 0); Vector3 leftEdge = new Vector3(0, -1, 0);

                topEdge = Vector3.Transform(topEdge, objectToTest.RotationMatrix);
                leftEdge = Vector3.Transform(leftEdge, objectToTest.RotationMatrix);

                Vector3 planeNormal = Vector3.Cross(leftEdge, topEdge);
                Plane spritePlane;

                spritePlane = new Plane(planeNormal.X, planeNormal.Y, planeNormal.Z,
                    planeNormal.X * objectToTest.X + planeNormal.Y * objectToTest.Y + planeNormal.Z * objectToTest.Z);

                float xTCoefLine = mouseRay.Direction.X;
                float yTCoefLine = mouseRay.Direction.Y;
                float zTCoefLine = mouseRay.Direction.Z;


                double t = (spritePlane.D - spritePlane.Normal.X * mouseRay.Position.X - spritePlane.Normal.Y * mouseRay.Position.Y - spritePlane.Normal.Z * mouseRay.Position.Z) /
                    (spritePlane.Normal.X * xTCoefLine + spritePlane.Normal.Y * yTCoefLine + spritePlane.Normal.Z * zTCoefLine);


                double xIntersect = mouseRay.Position.X + t * xTCoefLine;
                double yIntersect = mouseRay.Position.Y + t * yTCoefLine;
                double zIntersect = mouseRay.Position.Z + t * zTCoefLine;


                // Get the signed distance from the camera to plane perpendicular to the camera's view which contains
                // the intersection point.


                intersectionPoint.X = (float)xIntersect;
                intersectionPoint.Y = (float)yIntersect;
                intersectionPoint.Z = (float)zIntersect;

                // first get the intersection point relative to the camera
                Vector3 intersectionRelativeToCamera = intersectionPoint - camera.Position;

#if FRB_MDX
                // now project it onto the vector which shows which way the camera is viewing
                Vector3 lookingDirection = new Vector3(0, 0, 1);
                lookingDirection.TransformCoordinate(camera.RotationMatrix);

#else
                // now project it onto the vector which shows which way the camera is viewing
                Vector3 lookingDirection = Vector3.Transform(new Vector3(0, 0, 1), camera.RotationMatrix);
#endif

                float length = System.Math.Abs(Vector3.Dot(intersectionRelativeToCamera, lookingDirection));

                if (length < camera.NearClipPlane ||
                    length > camera.FarClipPlane)
                    return false;

#if FRB_MDX
                Matrix intersectMatrix = Matrix.Translation(
                    (float)(xIntersect - objectToTest.X),
                    (float)(yIntersect - objectToTest.Y),
                    (float)(zIntersect - objectToTest.Z));
#else
                Matrix intersectMatrix = Matrix.CreateTranslation(
                    (float)(xIntersect - objectToTest.X),
                    (float)(yIntersect - objectToTest.Y),
                    (float)(zIntersect - objectToTest.Z));
#endif
                intersectMatrix *= Matrix.Invert(objectToTest.RotationMatrix);

                if (System.Math.Abs(intersectMatrix.M41) < System.Math.Abs(objectToTest.ScaleX) &&
                    System.Math.Abs(intersectMatrix.M42) < System.Math.Abs(objectToTest.ScaleY))
                    return true;
                else
                    return false;

                #endregion
        }

        public static bool IsOrthonormal(ref Matrix matrix)
        {
            float epsilon = MatrixOrthonormalEpsilon;

            return System.Math.Abs(matrix.Right.LengthSquared() - 1) < epsilon &&
                System.Math.Abs(matrix.Up.LengthSquared() - 1) < epsilon &&
                System.Math.Abs(matrix.Forward.LengthSquared() - 1) < epsilon &&
                Vector3.Dot(matrix.Right, matrix.Up) < epsilon &&
                Vector3.Dot(matrix.Right, matrix.Forward) < epsilon &&
                Vector3.Dot(matrix.Up, matrix.Forward) < epsilon;
        }



        public static Vector3 Reflect(Vector3 vectorToReflect, Vector3 surfaceNormal)
        {
            surfaceNormal.Normalize();


            Vector3 projected = surfaceNormal * Vector3.Dot(vectorToReflect, surfaceNormal);

            return -(vectorToReflect - projected) + projected;

        }



        public static bool IsPointInsideRectangle(Point pointInQuestion, Point topLeft, Point topRight, Point bottomRight, Point bottomLeft)
        {
            double cross1Z = (topRight.X - topLeft.X) * (pointInQuestion.Y - topLeft.Y) -
                            (topRight.Y - topLeft.Y) * (pointInQuestion.X - topLeft.X);

            double cross2Z = (bottomRight.X - topRight.X) * (pointInQuestion.Y - topRight.Y) -
                            (bottomRight.Y - topRight.Y) * (pointInQuestion.X - topRight.X);

            double cross3Z = (bottomLeft.X - bottomRight.X) * (pointInQuestion.Y - bottomRight.Y) -
                            (bottomLeft.Y - bottomRight.Y) * (pointInQuestion.X - bottomRight.X);

            double cross4Z = (topLeft.X - bottomLeft.X) * (pointInQuestion.Y - bottomLeft.Y) -
                            (topLeft.Y - bottomLeft.Y) * (pointInQuestion.X - bottomLeft.X);

            return (cross1Z <= 0 && cross2Z <= 0 && cross3Z <= 0 && cross4Z <= 0);

        }


        public static bool IsPowerOfTwo(int numberToCheck)
        {
            return numberToCheck == 1 ||
                numberToCheck == 2 ||
                ((numberToCheck & (numberToCheck - 1)) == 0);
        }


        public static float Loop(float value, float loopPeriod)
        {
            if (loopPeriod == 0)
            {
                return 0;
            }

            int numberOfTimesIn = (int)(value / loopPeriod);

            if (value < 0)
            {
                return value + (1+numberOfTimesIn) * loopPeriod;
            }
            else
            {
                return value - numberOfTimesIn * loopPeriod;
            }
        }

        public static double Loop(double value, double loopPeriod)
        {
            if (loopPeriod == 0)
            {
                return 0;
            }

            int numberOfTimesIn = (int)(value / loopPeriod);

            if (value < 0)
            {
                return value + (1 + numberOfTimesIn) * loopPeriod;
            }
            else
            {
                return value - numberOfTimesIn * loopPeriod;
            }
        }

        public static float Loop(float value, float loopPeriod, out bool didLoop)
        {
            didLoop = false;
            if (loopPeriod == 0)
            {
                return 0;
            }

            int numberOfTimesIn = (int)(value / loopPeriod);
            if (value < 0)
            {
                didLoop = true;
                return value + (1 + numberOfTimesIn) * loopPeriod;
            }
            else
            {
                didLoop = numberOfTimesIn > 0;
                return value - numberOfTimesIn * loopPeriod;
            }
        }

        public static double Loop(double value, double loopPeriod, out bool didLoop)
        {
            didLoop = false;
            if (loopPeriod == 0)
            {
                return 0;
            }

            int numberOfTimesIn = (int)(value / loopPeriod);
            if (value < 0)
            {
                didLoop = true;
                return value + (1 + numberOfTimesIn) * loopPeriod;
            }
            else
            {
                didLoop = numberOfTimesIn > 0;
                return value - numberOfTimesIn * loopPeriod;
            }
        }

        public static int LoopInt(int value, int maxValueExclusive)
        {
            if (value == 0)
            {
                return 0;
            }
            else if (value > 0)
            {
                return value % maxValueExclusive;
            }
            else
            {
                var negativeModValue = (value % maxValueExclusive);
                if (negativeModValue == 0)
                {
                    return 0;
                }
                else
                {
                    return maxValueExclusive + negativeModValue;
                }
            }
        }

        public static float MaxAbs(float value1, float value2)
        {
            var absValue1 = System.Math.Abs(value1);
            var absValue2 = System.Math.Abs(value2);

            if(absValue1 > absValue2)
            {
                return value1;
            }
            else
            {
                return value2;
            }
        }

        public static int NextPowerOfTwo(int startingValue)
        {
            int nextPowerOf2 = 1;
            // Double powof2 until >= val
            while (nextPowerOf2 < startingValue) nextPowerOf2 <<= 1;

            return nextPowerOf2;
        }


        public static float RegulateAngle(float angleToRegulate)
        {
            angleToRegulate = angleToRegulate % ((float)System.Math.PI * 2);
            while (angleToRegulate < 0)
                angleToRegulate += (float)System.Math.PI * 2;
            return angleToRegulate;
        }

        public static double RegulateAngle(double angleToRegulate)
        {
            angleToRegulate = angleToRegulate % ((float)System.Math.PI * 2);
            while (angleToRegulate < 0)
                angleToRegulate += (float)System.Math.PI * 2;
            return angleToRegulate;
        }

        #region XML Docs
        /// <summary>
        /// Keeps an angle between 0 and 2*PI.
        /// </summary>
        /// <param name="angleToRegulate">The angle to regulate.</param>
        #endregion
        public static void RegulateAngle(ref float angleToRegulate)
        {
            angleToRegulate = angleToRegulate % ((float)System.Math.PI * 2);
            if (angleToRegulate < 0)
                angleToRegulate += (float)System.Math.PI * 2;
        }

        #region XML Docs
        /// <summary>
        /// Rotates a point around another point by a given number of radians.
        /// </summary>
        /// <param name="xBase">X position to rotate around.</param>
        /// <param name="yBase">Y position to rotate around.</param>
        /// <param name="xToRotate">X position to rotate (changes).</param>
        /// <param name="yToRotate">Y position to rotate (changes).</param>
        /// <param name="radiansToChangeBy">Radians to rotate by.</param>
        #endregion
        public static void RotatePointAroundPoint(float xBase, float yBase, ref float xToRotate, ref float yToRotate, float radiansToChangeBy)
        {

            double xDistance = xToRotate - xBase;
            double yDistance = yToRotate - yBase;
            if (xDistance == 0 && yDistance == 0)
                return;
            double distance = xDistance * xDistance + yDistance * yDistance;
            distance = System.Math.Pow(distance, .5);

            double angle = System.Math.Atan2(yDistance, xDistance);
            angle += radiansToChangeBy;

            xToRotate = (float)(System.Math.Cos(angle) * distance + xBase);
            yToRotate = (float)(System.Math.Sin(angle) * distance + yBase);
        }

        #region XML Docs
        /// <summary>
        /// Rotates a Point around another Point by a given number of radians.
        /// </summary>
        /// <param name="basePoint">Point to rotate around.</param>
        /// <param name="pointToRotate">Point to rotate (changes position).</param>
        /// <param name="radiansToChangeBy">Radians to rotate by.</param>
        #endregion
        public static void RotatePointAroundPoint(Point basePoint, ref Point pointToRotate, float radiansToChangeBy)
        {
            double xDistance = pointToRotate.X - basePoint.X;
            double yDistance = pointToRotate.Y - basePoint.Y;
            if (xDistance == 0 && yDistance == 0)
                return;

            double distance = xDistance * xDistance + yDistance * yDistance;
            distance = (float)System.Math.Pow(distance, .5);

            double angle = System.Math.Atan(yDistance / xDistance);
            if (xDistance < 0.0f) angle += (float)System.Math.PI;
            angle += radiansToChangeBy;

            pointToRotate.X = System.Math.Cos(angle) * distance + basePoint.X;
            pointToRotate.Y = System.Math.Sin(angle) * distance + basePoint.Y;

        }

        public static void RotatePointAroundPoint(Vector3 basePoint, ref Vector3 pointToRotate, float radiansToChangeBy)
        {
            double xDistance = pointToRotate.X - basePoint.X;
            double yDistance = pointToRotate.Y - basePoint.Y;
            if (xDistance == 0 && yDistance == 0)
                return;

            double distance = xDistance * xDistance + yDistance * yDistance;
            distance = (float)System.Math.Pow(distance, .5);

            double angle = System.Math.Atan2(yDistance , xDistance);
            angle += radiansToChangeBy;

            pointToRotate.X = (float)(System.Math.Cos(angle) * distance + basePoint.X);
            pointToRotate.Y = (float)(System.Math.Sin(angle) * distance + basePoint.Y);

        }

        /// <summary>
        /// Returns a value which has been rounded to the nearest mulitple of the mulipleOf value.
        /// </summary>
        /// <param name="valueToRound">The value to round, such as the position of an object.</param>
        /// <param name="multipleOf">The multiple of value, such as the size of a tile.</param>
        /// <returns>The rounded value.</returns>
        public static float RoundFloat(float valueToRound, float multipleOf)
        {

            return ((int)( System.Math.Sign(valueToRound) * .5f + valueToRound  / multipleOf)) * multipleOf;
        }

        public static float RoundFloat(float valueToRound, float multipleOf, float seed)
        {
            valueToRound -= seed;

            return seed + ((int)( System.Math.Sign(valueToRound) * .5f + valueToRound  / multipleOf)) * multipleOf;
        }

        public static double RoundDouble(double valueToRound, double multipleOf)
        {
            return ((int)( System.Math.Sign(valueToRound) * .5 + valueToRound  / multipleOf)) * multipleOf;
        }

        public static double RoundDouble(double valueToRound, double multipleOf, double seed)
        {
            valueToRound -= seed;

            return seed + ((int)(System.Math.Sign(valueToRound) * .5f + valueToRound / multipleOf)) * multipleOf;
        }

        /// <summary>
        /// Rounds the argument floatToRound to an integer.
        /// </summary>
        /// <param name="floatToRound">The float value.</param>
        /// <returns>The rounded value as an integer.</returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int RoundToInt(float floatToRound)
        {
            // System.Math.Round should give us a number very close to the decimal
            // Of course, it may give us something like 3.99999, which would become
            // 3 when converted to an int.  We want that to be 4, so we add a small amount
            // But...why .5 and not something smaller?  Well, we want to minimize error due
            // to floating point inaccuracy, so we add a value that is big enough to get us into
            // the next integer value even when the float is really large (has a lot of error).
            return (int)(System.Math.Round(floatToRound) + (System.Math.Sign(floatToRound) * .5f));

        }

        /// <summary>
        /// Rounds the argument doubleToRound to an integer.
        /// </summary>
        /// <param name="doubleToRound">The double value.</param>
        /// <returns>The rounded value as an integer.</returns>
        public static int RoundToInt(double doubleToRound)
        {
            // see the other RoundToInt for information on why we add .5
            return (int)(System.Math.Round(doubleToRound) + (System.Math.Sign(doubleToRound) * .5));
        }

        /// <summary>
        /// Rounds the argument decimalToRound to an integer.
        /// </summary>
        /// <param name="decimalToRound">The decimal value.</param>
        /// <returns>The rounded value as a decimal.</returns>
        public static int RoundToInt(decimal decimalToRound) =>
            (int)(System.Math.Round(decimalToRound) + (System.Math.Sign(decimalToRound)* .5m));

        public static long RoundToLong(double doubleToRound)
        {
            // see the other RoundToInt for information on why we add .5
            return (long)(System.Math.Round(doubleToRound) + (System.Math.Sign(doubleToRound) * .5f));
        }


        public static bool AreFloatsEqual(float num1, float num2, float epsilon)
        {
            return System.Math.Abs(num1 - num2) <= epsilon;
        }

        public static bool AreDoublesEqual(double num1, double num2, double epsilon)
        {
            return System.Math.Abs(num1 - num2) <= epsilon;
        }

#if FRB_MDX
        public static void ScreenToAbsolute(int screenX, int screenY, ref float x,
            ref float y, float z, Camera camera, System.Windows.Forms.Control form, bool fullscreen)
        {
            System.Drawing.Point point = new System.Drawing.Point(screenX, screenY);
            point = form.PointToClient(point);
            if (fullscreen)
            {
                // maximized, so scale the position
                point.X = (int)(screenX * form.ClientRectangle.Width /
                    (float)System.Windows.Forms.SystemInformation.PrimaryMonitorSize.Width
                    );
                point.Y = (int)(screenY * form.ClientRectangle.Height /
                    (float)System.Windows.Forms.SystemInformation.PrimaryMonitorSize.Height
                    );
            }

            x = camera.XEdge * (point.X * 2.0f - form.ClientRectangle.Width) / form.ClientRectangle.Width;
            y = -camera.YEdge * (point.Y * 2.0f - form.ClientRectangle.Height) / form.ClientRectangle.Height;
        }
#endif        


        public static void WindowToAbsolute(int screenX, int screenY, IPositionable positionable)
        {
            Vector3 position = new Vector3(positionable.X, positionable.Y, positionable.Z);

            WindowToAbsolute(screenX, screenY, ref position);

            positionable.X = position.X;
            positionable.Y = position.Y;
            positionable.Z = position.Z;

        }

        public static void WindowToAbsolute(int screenX, int screenY, ref Vector3 position)
        {
            float worldX = 0;
            float worldY = 0;
            float worldZ = position.Z;

            WindowToAbsolute(
               screenX,
               screenY,
               ref worldX,
               ref worldY,
               worldZ,
               SpriteManager.Camera,
               FlatRedBall.Camera.CoordinateRelativity.RelativeToWorld
            );

            position.X = worldX;
            position.Y = worldY;

        }

        public static void WindowToAbsolute(int screenX, int screenY, ref float x, 
            ref float y, float z, Camera camera, FlatRedBall.Camera.CoordinateRelativity coordinateRelativity)
		{
            

            /*
            if (fullscreen)
            {
                // maximized, so scale the position
                point.X = (int)(screenX*form.ClientRectangle.Width/
                    (float)System.Windows.Forms.SystemInformation.PrimaryMonitorSize.Width
                    );
                point.Y = (int)(screenY * form.ClientRectangle.Height /
                    (float)System.Windows.Forms.SystemInformation.PrimaryMonitorSize.Height
                    );
            }
 */
            if (camera.Orthogonal)
            {
                float top = camera.AbsoluteTopYEdgeAt(z);
                float left = camera.AbsoluteLeftXEdgeAt(z);

                float distanceFromLeft = camera.OrthogonalWidth * screenX / (float)camera.DestinationRectangle.Width;
                float distanceFromTop = -camera.OrthogonalHeight * screenY / (float)camera.DestinationRectangle.Height;

                x = left + distanceFromLeft;
                y = top + distanceFromTop;
            }
            else
            {
                WindowToAbsolute(screenX, screenY, ref x, ref y, z, camera.Position, camera.XEdge,
                    camera.YEdge, camera.DestinationRectangle, coordinateRelativity);

                if (camera.RotationZ != 0)
                {
                    RotatePointAroundPoint(camera.X, camera.Y, ref x, ref y, camera.RotationZ);
                }
            }

        }


        public static void WindowToAbsolute(int screenX, int screenY, ref float x,
            ref float y, float z, Vector3 cameraPosition, float cameraXEdge, float cameraYEdge,
            Rectangle destinationRectangle,
            FlatRedBall.Camera.CoordinateRelativity coordinateRelativity)
        {
            if (coordinateRelativity == Camera.CoordinateRelativity.RelativeToCamera)
            {
                x = (FlatRedBall.Math.MathFunctions.ForwardVector3.Z * z/100) * cameraXEdge *
                    (screenX * 2.0f - destinationRectangle.Width) / destinationRectangle.Width;
                y = (FlatRedBall.Math.MathFunctions.ForwardVector3.Z * z / 100) * -cameraYEdge *
                    (screenY * 2.0f - destinationRectangle.Height) / destinationRectangle.Height;
            }
            else
            {
                x = cameraPosition.X - FlatRedBall.Math.MathFunctions.ForwardVector3.Z * ((cameraPosition.Z - z) / 100.0f) * cameraXEdge *
                    (screenX * 2.0f - destinationRectangle.Width) / destinationRectangle.Width;
                y = cameraPosition.Y - FlatRedBall.Math.MathFunctions.ForwardVector3.Z * ((cameraPosition.Z - z) / 100.0f) * -cameraYEdge *
                    (screenY * 2.0f - destinationRectangle.Height) / destinationRectangle.Height;
            }
        }



        public static void ScreenToAbsoluteDistance(int pixelX, int pixelY, out float x, out float y, float z, Camera camera)
        {
            x = (MathFunctions.ForwardVector3.Z * (z - camera.Z) * camera.XEdge / 100.0f) * 2 * (float)pixelX / (float)FlatRedBallServices.ClientWidth;
            // Y is inverted, so we multiply by negative 1
            y = (MathFunctions.ForwardVector3.Z * (z - camera.Z) * camera.YEdge / 100.0f) * 2 * (float)pixelY / (float)FlatRedBallServices.ClientHeight;
        }

        public static void SortAscending(List<int> integerList)
        {
            if (integerList.Count == 1 || integerList.Count == 0)
                return;

            int whereSpriteBelongs;

            for (int i = 1; i < integerList.Count; i++)
            {
                if ((integerList[i]) < (integerList[i - 1]))
                {
                    if (i == 1)
                    {
                        integerList.Insert(0, integerList[i]);
                        integerList.RemoveAt(i + 1);
                        continue;
                    }

                    for (whereSpriteBelongs = i - 2; whereSpriteBelongs > -1; whereSpriteBelongs--)
                    {
                        if ((integerList[i]) >= (integerList[whereSpriteBelongs]))
                        {
                            integerList.Insert(whereSpriteBelongs + 1, integerList[i]);
                            integerList.RemoveAt(i + 1);
                            break;
                        }
                        else if (whereSpriteBelongs == 0 && (integerList[i]) < (integerList[0]))
                        {
                            integerList.Insert(0, integerList[i]);
                            integerList.RemoveAt(i + 1);
                            break;
                        }
                    }
                }
            }
        }


        public static void TransformVector(ref Vector2 vectorToTransform, ref Matrix matrixToTransformBy)
        {
            sMethodCallVector3.X =
                matrixToTransformBy.M11 * vectorToTransform.X +
                matrixToTransformBy.M21 * vectorToTransform.Y;

            sMethodCallVector3.Y =
                matrixToTransformBy.M12 * vectorToTransform.X +
                matrixToTransformBy.M22 * vectorToTransform.Y;

            vectorToTransform.X = sMethodCallVector3.X;
            vectorToTransform.Y = sMethodCallVector3.Y;
        }


        public static void TransformVector(ref Vector3 vectorToTransform, ref Matrix matrixToTransformBy)
        {
            // We use this in threaded apps so we have to have a local var :(
            Vector3 temp = vectorToTransform;

            temp.X =
                matrixToTransformBy.M11 * vectorToTransform.X +
                matrixToTransformBy.M21 * vectorToTransform.Y +
                matrixToTransformBy.M31 * vectorToTransform.Z +
                matrixToTransformBy.M41;

            temp.Y =
                matrixToTransformBy.M12 * vectorToTransform.X +
                matrixToTransformBy.M22 * vectorToTransform.Y +
                matrixToTransformBy.M32 * vectorToTransform.Z +
                matrixToTransformBy.M42;

            temp.Z =
                matrixToTransformBy.M13 * vectorToTransform.X +
                matrixToTransformBy.M23 * vectorToTransform.Y +
                matrixToTransformBy.M33 * vectorToTransform.Z +
                matrixToTransformBy.M43;

            vectorToTransform = temp;
        }

        static Point3D sMethodCallPoint3D;

        public static void TransformVector(ref Point3D pointToTransform, ref Matrix matrixToTransformBy)
        {
            sMethodCallPoint3D.X =
                matrixToTransformBy.M11 * pointToTransform.X +
                matrixToTransformBy.M21 * pointToTransform.Y +
                matrixToTransformBy.M31 * pointToTransform.Z;

            sMethodCallPoint3D.Y =
                matrixToTransformBy.M12 * pointToTransform.X +
                matrixToTransformBy.M22 * pointToTransform.Y +
                matrixToTransformBy.M32 * pointToTransform.Z;

            sMethodCallPoint3D.Z =
                matrixToTransformBy.M13 * pointToTransform.X +
                matrixToTransformBy.M23 * pointToTransform.Y +
                matrixToTransformBy.M33 * pointToTransform.Z;

            pointToTransform = sMethodCallPoint3D;
        }

        public static Vector3 TransformVector(Vector3 vectorToTransform, Matrix matrixToTransformBy)
        {
            sMethodCallVector3.X =
                matrixToTransformBy.M11 * vectorToTransform.X +
                matrixToTransformBy.M21 * vectorToTransform.Y +
                matrixToTransformBy.M31 * vectorToTransform.Z;

            sMethodCallVector3.Y =
                matrixToTransformBy.M12 * vectorToTransform.X +
                matrixToTransformBy.M22 * vectorToTransform.Y +
                matrixToTransformBy.M32 * vectorToTransform.Z;

            sMethodCallVector3.Z =
                matrixToTransformBy.M13 * vectorToTransform.X +
                matrixToTransformBy.M23 * vectorToTransform.Y +
                matrixToTransformBy.M33 * vectorToTransform.Z;

            return sMethodCallVector3;
        }


        private static Point sMethodCallPoint;
        public static void TransformPoint(ref Point pointToTransform, ref Matrix matrixToTransformBy)
        {
            sMethodCallPoint.X =
                matrixToTransformBy.M11 * pointToTransform.X +
                matrixToTransformBy.M21 * pointToTransform.Y;

            sMethodCallPoint.Y =
                matrixToTransformBy.M12 * pointToTransform.X +
                matrixToTransformBy.M22 * pointToTransform.Y;

            pointToTransform.X = sMethodCallPoint.X;
            pointToTransform.Y = sMethodCallPoint.Y;
        }


        public static Matrix LerpRotationMatrix(Matrix currentRotationMatrix, Matrix destinationRotationMatrix, float amount)
        {
            // Create quaternions
            Quaternion a = Quaternion.CreateFromRotationMatrix(currentRotationMatrix);
            Quaternion b = Quaternion.CreateFromRotationMatrix(destinationRotationMatrix);

            // Lerp and return
            return Matrix.CreateFromQuaternion(
                Quaternion.Lerp(a, b, amount));
        }

        #region XML Docs
        /// <summary>
        /// Wraps an angle from 0 to TwoPi
        /// </summary>
        /// <param name="angle">The angle to wrap</param>
        /// <returns>The new angle</returns>
        #endregion
        public static float NormalizeAngle(float angle)
        {
            while (angle < 0f) angle += MathHelper.TwoPi;
            while (angle >= MathHelper.TwoPi) angle -= MathHelper.TwoPi;

            return angle;
        }

        public static float MoveTowardAngle(float currentAngle, float destinationAngle, float amount)
        {
            amount = System.Math.Abs(amount);

            float angle, destAngle;
            angle = NormalizeAngle(currentAngle);
            destAngle = NormalizeAngle(destinationAngle);

            // Return if normalized angles are equal
            if (angle == destAngle)
            {
                return destinationAngle;
            }

            // Otherwise, get direction and distance to move
            float direction = 1f;
            float distance = 0f;

            #region Get Direction and Distance

            if (destAngle > angle)
            {
                // Check if the angle is closer in the negative direction than positive
                if (angle - (destAngle - MathHelper.TwoPi) < destAngle - angle)
                {
                    direction = -1f;
                    distance = angle - (destAngle - MathHelper.TwoPi);
                }
                else
                {
                    distance = destAngle - angle;
                }
            }
            else
            {
                // Check if the angle is closer in the positive direction than negative
                if (destAngle - (angle - MathHelper.TwoPi) < angle - destAngle)
                {
                    distance = destAngle - (angle - MathHelper.TwoPi);
                }
                else
                {
                    direction = -1f;
                    distance = angle - destAngle;
                }
            }

            #endregion

            // Move
            if (distance < amount)
            {
                return destinationAngle;
            }
            else
            {
                return currentAngle + amount * direction;
            }
        }

        public static float Hypotenuse(float X, float Y)
        {
            return (float)System.Math.Sqrt(X*X + Y*Y);
        }
        
        public static double Hypotenuse(double X, double Y)
        {
            return System.Math.Sqrt(X * X + Y * Y);
        }

        public static float HypotenuseSquared(float X, float Y)
        {
            return (X * X + Y * Y);
        }

        public static Vector3 AngleToVector(float radians)
        {
            return new Vector3((float)System.Math.Cos((double)radians), (float)System.Math.Sin((double)radians), 0f);
        }

        #endregion
    }
}
