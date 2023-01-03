using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Math.Geometry;
using Microsoft.Xna.Framework;

namespace FlatRedBall.Utilities
{
    /// <summary>
    /// Class deriving from Random providing additional random methods commonly used in games. Typically this is accessed
    /// through FlatRedBallServices.Random, but can also be instantiated manually.
    /// </summary>
    public class GameRandom : Random
    {
        /// <summary>
        /// Instantiates a new GameRandom creating a 
        /// </summary>
        public GameRandom() : base() { }

        /// <summary>
        /// Instantiates a new instance of the GameRandom class using the specified seed.
        /// </summary>
        /// <param name="seed">The seed used to initialize the random sequence of numbers.</param>
        public GameRandom(int seed) : base(seed) { }

        /// <summary>
        /// Returns a random element in a list.
        /// </summary>
        /// <typeparam name="T">The list type.</typeparam>
        /// <param name="list">The list to return an element from.</param>
        /// <returns>A random element, obtained by using the Next method to obtain a random index.</returns>
        public T In<T>(IList<T> list)
        {
#if DEBUG
            if(list == null)
            {
                throw new ArgumentNullException("list argument cannot be null");
            }
            if(list.Count == 0)
            {
                throw new InvalidOperationException("Cannot get a random element from an empty list");
            }
#endif
            return list[Next(list.Count)];
        }

        /// <summary>
        /// Returns multiple instances from an argument list, guaranteeing
        /// no duplicates.
        /// </summary>
        /// <typeparam name="T">The type of the list.</typeparam>
        /// <param name="list">The list to pull from"</param>
        /// <param name="numberToReturn">The number of unique items to return, which must be less than the size of the argument list</param>
        /// <returns>A resulting collection of size numberToReturn</returns>
        public IList<T> MultipleIn<T>(IList<T> list, int numberToReturn)
        {
#if DEBUG
            if(numberToReturn > list.Count)
            {
                throw new ArgumentException(
                    $"Cannot return {numberToReturn} because the list only has {list.Count} elements");
            }
#endif
            var remaining = list.ToList();
            List<T> toReturn = new List<T>();
            for(int i = 0; i < numberToReturn; i++)
            {
                var newIndex = Next(remaining.Count);
                toReturn.Add(remaining[newIndex]);
                remaining.RemoveAt(newIndex);
            }

            return toReturn;
        }

        /// <summary>
        /// Returns a random number within the specified range (inclusive).
        /// </summary>
        /// <param name="lowerBound">The inclusive lower bound.</param>
        /// <param name="upperBound">The inclusive upper bound</param>
        /// <returns>The random float between the bounds.</returns>
        public float Between(float lowerBound, float upperBound) => lowerBound + (float)NextDouble() * (upperBound - lowerBound);

        public decimal Between(decimal lowerBound, decimal upperBound) => lowerBound + (decimal)NextDouble() * (upperBound - lowerBound);


        /// <summary>
        /// Returns a random number within the specified range, where the lower bound is inclusive but the upper bound is exclusive.
        /// </summary>
        /// <param name="lowerInclusive">An inclusive lower bound - the number specified here can be returned.</param>
        /// <param name="upperExclusive">An exclusive upper bound - the number specified here will never be returned - the largest number possible is one less.</param>
        /// <returns>A random number inbetween the inclusive lower and exclusive upper bound.</returns>
        public int Between(int lowerInclusive, int upperExclusive) =>
            lowerInclusive + Next(upperExclusive - lowerInclusive);

        /// <summary>
        /// Returns a random angle in radians (between 0 and 2*Pi).
        /// </summary>
        /// <returns>A random angle in radians.</returns>
        public float AngleRadians() => (float)NextDouble() * MathHelper.TwoPi;

        /// <summary>
        /// Returns a random angle in degrees (0 to 360)
        /// </summary>
        /// <returns>A random angle in degrees.</returns>
        public float AngleDegrees() => (float)NextDouble() * 360;

        /// <summary>
        /// Returns a 2-dimensional vector in a random direction with length within
        /// the specified range. A higher distribution will occur around the lower-end.
        /// </summary>
        /// <param name="minLength">The inclusive lower bound of the length.</param>
        /// <param name="maxLength">The inclusive upper bound of the length.</param>
        /// <returns>The resulting 2-dimensional vector.</returns>
        public Vector2 RadialVector2(float minLength, float maxLength)
        {
            var angle = AngleRadians();

            float length;
            
            length = Between(minLength, maxLength);

            return new Vector2(
                (float)System.Math.Cos((double)angle) * length, 
                (float)System.Math.Sin((double)angle) * length);
        }

        /// <summary>
        /// Returns a Vector2 of random length and angle between the argument values. A higher distribution will appear at the center of the wedge.
        /// </summary>
        /// <param name="minLength">The minimum length of the returned vector.</param>
        /// <param name="maxLength">The maximum length of the returned vector.</param>
        /// <param name="minRadians">The minimum angle in radians of the returned vector.</param>
        /// <param name="maxRadians">The maximum angle in radians of the returned vector.</param>
        /// <returns>A random vector using the argument values.</returns>
        public Vector2 WedgeVector2Radians(float minLength, float maxLength, float minRadians, float maxRadians)
        {
            var angle = Between(minRadians, maxRadians);
            var length = Between(minLength, maxLength);

            return new Vector2(
                (float)System.Math.Cos((double)angle) * length,
                (float)System.Math.Sin((double)angle) * length);
        }

        /// <summary>
        /// Returns a random point in a circle with uniform distribution (as opposed to higher distribution at the center).
        /// </summary>
        /// <param name="radius">The circle radius</param>
        /// <returns>The random point</returns>
        public Vector2 PointInCircle(float radius)
        {
            // this gets us a random distribution instead of bunching up at the center:

            var calculatedRadius = System.Math.Sqrt(Between(0.0f,1.0f)) * radius;

            var angle = AngleRadians();
            //var radius = _random.NextDouble() * _radius;


            var x = (float)(calculatedRadius * System.Math.Cos(angle));
            var y = (float)(calculatedRadius * System.Math.Sin(angle));
            return new Vector2(x, y);
        }

        /// <summary>
        /// Returns a Vector2 of random length and angle between the argument values. A higher distribution will appear at the center of the wedge.
        /// </summary>
        /// <param name="minLength">The minimum length of the returned vector.</param>
        /// <param name="maxLength">The maximum length of the returned vector.</param>
        /// <param name="minDegrees">The minimum angle in degrees of the returned vector.</param>
        /// <param name="maxDegrees">Teh maximum angle in degrees of the returned vector.</param>
        /// <returns>A random vector using the argument values.</returns>
        public Vector2 WedgeVector2Degrees(float minLength, float maxLength, float minDegrees, float maxDegrees)
        {
            var minRadians = MathHelper.ToRadians(minDegrees);
            var maxRadians = MathHelper.ToRadians(maxDegrees);

            return WedgeVector2Radians(minLength, maxLength, minRadians, maxRadians);
        }


        public bool NextBool() => Next(2) == 0;

        class CumulativeAreaRectangle
        {
            public AxisAlignedRectangle Rectangle;
            public float CumulativeSize;
        }

        public Vector2 PointIn(ShapeCollection shapeCollection)
        {
            if (shapeCollection.Circles.Count > 0)
            {
                throw new NotImplementedException("PointIn with Circles not implemented - bug Vic");
            }
            if(shapeCollection.Polygons.Count > 0)
            {
                throw new NotImplementedException("PointIn with Polygons not implemented - bug Vic");
            }

            var rectangles = new List<CumulativeAreaRectangle>();
            float cumulative = 0;
            for (int i = 0; i < shapeCollection.AxisAlignedRectangles.Count; i++)
            {
                var rectangle = shapeCollection.AxisAlignedRectangles[i];
                cumulative += rectangle.Width * rectangle.Height;
                rectangles.Add(new CumulativeAreaRectangle
                {
                    Rectangle = rectangle,
                    CumulativeSize = cumulative
                }) ;

            }

            var randomArea = Between(0, cumulative);
            AxisAlignedRectangle rectangleToUse = null;
            for(int i = 0; i < rectangles.Count; i++)
            {
                var cumulativeRectangle = rectangles[i];

                if(cumulativeRectangle.CumulativeSize >= randomArea)
                {
                    rectangleToUse = cumulativeRectangle.Rectangle;
                    break;
                }
            }

            if(rectangleToUse != null)
            {
                return rectangleToUse.GetRandomPositionInThis().ToVector2();
            }
            else
            {
                return Vector2.Zero;
            }

        }
    }
}
