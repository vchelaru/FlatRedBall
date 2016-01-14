using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Utilities;




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



#if FRB_XNA
        public static Plane ZPlane = new Plane(new Vector3(0, 0, 1), 0);

#endif

        #endregion

        public const float MatrixOrthonormalEpsilon = .001f;

        #region Methods

#if FRB_MDX
        public static void AbsoluteToScreen(float x, float y, float z, ref int screenX, ref int screenY, Camera camera, System.Windows.Forms.Control form)
        {
            float xRatioOver = (x - camera.X) / camera.XEdge;
            xRatioOver += 1;
            xRatioOver /= 2.0f;

            float yRatioOver = (y - camera.Y) / camera.YEdge;
            yRatioOver -= 1;
            yRatioOver /= -2.0f;


            // get the top left
            //            topLeftPoint = new System.Drawing.Point(form.ClientRectangle.Left, form.ClientRectangle.Top);

            float xPixelOver = xRatioOver * form.ClientRectangle.Width;
            float yPixelOver = yRatioOver * form.ClientRectangle.Height;

            System.Drawing.Point point = new System.Drawing.Point((int)xPixelOver, (int)yPixelOver);

            point = form.PointToScreen(point);

            screenX = point.X;




            screenY = point.Y;

        }
#endif

        #region XML Docs
        /// <summary>
        /// Determines the shortest absolute difference between two angles.
        /// </summary>
        /// <remarks>
        /// This method will never return a value with absolute value greater than PI.  It will return 
        /// either a positive or negative value, keeping all values between positive and negative PI.
        /// </remarks>
        /// <param name="angle1">Starting angle in radians.</param>
        /// <param name="angle2">Ending angle in radians.</param>
        /// <returns>The number of radians between the two angles.</returns>
        #endregion
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


        static int frameToFrameInteger;


        public static float GetAspectRatioForSameSizeAtResolution(float resolution)
        {
            double yAt600 = System.Math.Sin(System.Math.PI / 8.0);
            double xAt600 = System.Math.Cos(System.Math.PI / 8.0);

            double desiredYAt600 = yAt600 * (double)resolution / 600.0;
            float desiredAngle = (float)System.Math.Atan2(desiredYAt600, xAt600);

            return 2 * desiredAngle; 
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




#if FRB_XNA
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
#endif
#if !SILVERLIGHT

        // September 8, 2013
        // These seem like old
        // functions that we don't
        // use anymore.  I'm going to
        // comment them out to see if it
        // causes any problems.
        //public static bool IsPointInside<T>(float xPosition, float yPosition, T scalablePositionable) where T : IScalable, IPositionable
        //{
        //    return IsPointInside(xPosition, yPosition, scalablePositionable, ref SpriteSelectionOptions.Default);
        //}

        //public static bool IsPointInside<T>(float xPosition, float yPosition, T scalablePositionable, ref SpriteSelectionOptions spriteSelectionOptions) where T : IScalable, IPositionable
        //{
        //    return  xPosition > scalablePositionable.X - scalablePositionable.ScaleX + spriteSelectionOptions.LeftAllowance &&
        //            xPosition < scalablePositionable.X + scalablePositionable.ScaleX - spriteSelectionOptions.RightAllowance &&
        //            yPosition > scalablePositionable.Y - scalablePositionable.ScaleY + spriteSelectionOptions.BottomAllowance &&
        //            yPosition < scalablePositionable.Y + scalablePositionable.ScaleY - spriteSelectionOptions.TopAllowance;
        //}
#endif




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

        public static int RoundToInt(double doubleToRound)
        {
            // see the other RoundToInt for information on why we add .5
            return (int)(System.Math.Round(doubleToRound) + (System.Math.Sign(doubleToRound) * .5f));
        }

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




#if FRB_XNA
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

#endif
        #endregion
    }
}
