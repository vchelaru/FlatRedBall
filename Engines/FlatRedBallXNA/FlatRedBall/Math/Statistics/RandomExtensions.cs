using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Math.Statistics
{
	public static class RandomExtensions
	{
		public static float NextFloat(this Random random)
		{
			return (float)random.NextDouble();
		}

		/// <summary>
		/// Returns a float value between 0 and the argument maxValue;
		/// </summary>
        /// <param name="random">The random instance to use.</param>
		/// <param name="maxValue">The max value to return.</param>
		/// <returns>A value between 0 and the max value.</returns>
		public static float NextFloat(this Random random, float maxValue)
		{
			return (float)(random.NextDouble() * maxValue);
		}
	}
}
