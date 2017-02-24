using System;
using System.Collections.Generic;
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
        public T In<T>(IList<T> list) => list[Next(list.Count)];

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

    }
}
