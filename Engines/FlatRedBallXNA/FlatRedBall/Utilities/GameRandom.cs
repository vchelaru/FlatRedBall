using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace FlatRedBall.Utilities
{
    public class GameRandom : Random
    {
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
            for(int i = 0; i < remaining.Count; i++)
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
        /// the specified range.
        /// </summary>
        /// <param name="minLength">The inclusive lower bound of the length.</param>
        /// <param name="maxLength">The inclusive upper bound of the length.</param>
        /// <returns>The resulting 2-dimensional vector.</returns>
        public Vector2 RadialVector2(float minLength, float maxLength)
        {
            var angle = AngleRadians();
            var length = Between(minLength, maxLength);

            return new Vector2(
                (float)System.Math.Cos((double)angle) * length, 
                (float)System.Math.Sin((double)angle) * length);
        }

        /// <summary>
        /// Returns a Vector2 of random length and angle between the argument values. 
        /// </summary>
        /// <param name="minLength">The minimum length of the returned vector.</param>
        /// <param name="maxLength">The maximum length of the returned vector.</param>
        /// <param name="minRadians">The minimum angle of the returned vector.</param>
        /// <param name="maxRadians">The maximum angle of the returned vector.</param>
        /// <returns>A random vector using the argument values.</returns>
        public Vector2 WedgeVector2Radians(float minLength, float maxLength, float minRadians, float maxRadians)
        {
            var angle = Between(minRadians, maxRadians);
            var length = Between(minLength, maxLength);

            return new Vector2(
                (float)System.Math.Cos((double)angle) * length,
                (float)System.Math.Sin((double)angle) * length);
        }


    }
}
