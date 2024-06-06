using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Math
{
    /// <summary>
    /// A class which can be used to store and calculate rolling averages for regular numbers and radians.
    /// </summary>
    public class RollingAverage
    {
        #region Fields

        int mCapacity;

        List<float> mValues;

        bool mIsRadian;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the rolling average using the values currently stored, or 0 if no values have been added.
        /// </summary>
        /// <remarks>
        /// The rolling average is calculated using the Capacity number of
        /// values.  If AddValue has not been called enough times to fill the
        /// Capacity, then the number of values stored are used.
        /// </remarks>
        public float Average
        {
            get
            {
                #region If is radian
                if (mIsRadian)
                {
                    if (mValues.Count == 0)
                    {
                        return 0;
                    }
                    else
                    {
                        float summedAverage = mValues[0];

                        for (int i = 1; i < mValues.Count; i++)
                        {
                            summedAverage +=
                                Math.MathFunctions.AngleToAngle(summedAverage, mValues[i]) / (i + 1);

                            MathFunctions.RegulateAngle(ref summedAverage);
                        }

                        return summedAverage;
                    }
                }
                #endregion

                #region Else, not radian
                else
                {
                    if (mValues.Count == 0)
                    {
                        return 0;
                    }
                    else
                    {
                        float sum = 0;
                        for (int i = 0; i < mValues.Count; i++)
                        {
                            sum += mValues[i];
                        }

                        return sum / (float)mValues.Count;
                    }
                }
                #endregion
            }
        }

        /// <summary>
        /// Gets the number of values that are used when calculating a rolling average.
        /// </summary>
        public int Capacity
        {
            get { return mCapacity; }
            set 
            { 
                if(value != mCapacity)
                {
                    mCapacity = value;
                    while (mValues.Count > value)
                    {
                        mValues.RemoveAt(0);
                    }
                }
            
            }
        }


        public int Count
        {
            get { return mValues.Count; }
        }

        /// <summary>
        /// Gets and sets whether the average value is calculated as radians.
        /// </summary>
        /// <remarks>
        /// This is important for radian values because rotation values reported
        /// by FlatRedBall loop every 2*PI
        /// </remarks>
        public bool IsRadian
        {
            get { return mIsRadian; }
            set { mIsRadian = value; }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates a new RolllingAverage with capacity equalling the argument capacity value.
        /// </summary>
        /// <param name="capacity">The maximum number of values that the RollingAverage can store.</param>
        public RollingAverage(int capacity)
        {
            mCapacity = capacity;

            mValues = new List<float>(capacity);
        }

        /// <summary>
        /// Adds a value to the RollingAverage.  The oldest value is discarded if the Capacity has been reached. 
        /// </summary>
        /// <param name="value">The value to add.</param>
        public void AddValue(float value)
        {
            if (IsRadian)
            {
                MathFunctions.RegulateAngle(ref value);
            }

            mValues.Add(value);

            if (mValues.Count > mCapacity)
            {
                mValues.RemoveAt(0);
            }
        }

        /// <summary>
        /// Clears all values from the rolling average.
        /// </summary>
        public void Clear()
        {
            mValues.Clear();
        }

        #endregion
    }
}
