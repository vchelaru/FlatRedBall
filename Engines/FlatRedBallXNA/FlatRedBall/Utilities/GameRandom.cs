using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;

namespace FlatRedBall.Utilities
{
    public class GameRandom
    {

        Random internalRandom;

        /// <summary>
        /// Initializes a new instance of the System.Random class, using a time-dependent
        /// default seed value.
        /// </summary>
        public GameRandom()
        {
            internalRandom = new Random();
        }

        /// <summary>
        /// Initializes a new instance of the System.Random class, using the specified seed value.
        /// </summary>
        /// <param name="seed">A number used to calculate a starting value for the pseudo-random number sequence.
        /// If a negative number is specified, the absolute value of the number is used.</param>
        public GameRandom(int seed)
        {
            internalRandom = new Random(seed);
        }

        /// <summary>
        /// Returns a nonnegative random number.
        /// </summary>
        /// <returns>A 32-bit signed integer greater than or equal to zero and less than System.Int32.MaxValue.</returns>
        public int Next() => internalRandom.Next();

        /// <summary>
        /// Returns a nonnegative random number less than the specified maximum.
        /// </summary>
        /// <param name="maxValue">
        /// The exclusive upper bound of the random number to be generated. maxValue must
        /// be greater than or equal to zero.</param>
        /// <returns>A 32-bit signed integer greater than or equal to zero, and less than maxValue;
        /// that is, the range of return values ordinarily includes zero but not maxValue.
        /// However, if maxValue equals zero, maxValue is returned.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">maxValue is less than zero.</exception>"
        public int Next(int maxValue) => internalRandom.Next(maxValue);

        /// <summary>
        /// Returns a random number within a specified range.
        /// </summary>
        /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
        /// <param name="maxValue">The exclusive upper bound of the random number returned. maxValue must be greater than or equal to minValue.</param>
        /// <returns> A 32-bit signed integer greater than or equal to minValue and less than maxValue; that is, the range of return values includes minValue but not maxValue. If minValue equals maxValue, minValue is returned.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">minValue is greater than maxValue.</exception>"
        public int Next(int minValue, int maxValue) => internalRandom.Next(minValue, maxValue);

        /// <summary>
        /// Returns a random number between 0.0 and 1.0.
        /// </summary>
        /// <returns>A double-precision floating point number greater than or equal to 0.0, and less
        /// than 1.0.</returns>
        public double NextDouble() => internalRandom.NextDouble();

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
        public float Between(float lowerBound, float upperBound) => lowerBound + (float)internalRandom.NextDouble() * (upperBound - lowerBound);

        /// <summary>
        /// Returns a random angle in radians (between 0 and 2*Pi).
        /// </summary>
        /// <returns>A random angle in radians.</returns>
        public float AngleRadians() => (float)internalRandom.NextDouble() * MathHelper.TwoPi;

        /// <summary>
        /// Returns a random angle in degrees (0 to 360)
        /// </summary>
        /// <returns>A random angle in degrees.</returns>
        public float AngleDegrees() => (float)internalRandom.NextDouble() * 360;


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
                (float)System.Math.Cos((double)angle * length), 
                (float)System.Math.Sin((double)angle * length));
        }

    }
}
