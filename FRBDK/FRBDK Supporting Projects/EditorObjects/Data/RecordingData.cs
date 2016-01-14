using System;
using System.Collections.Generic;
using System.Text;
using FlatRedBall.Math.Statistics;

namespace EditorObjects.Data
{
    public class RecordingData
    {
        #region Fields

        List<double> mBeatTimes = new List<double>();
        LinearRegression mLinearRegression = new LinearRegression();

        #endregion

        #region Properties

        public int BeatCount
        {
            get
            {
                return mBeatTimes.Count;
            }
        }

        //public double BeatFrequency
        //{
        //    get
        //    {
        //        if (mBeatTimes.Count == 0)
        //        {
        //            return 0;
        //        }
        //        else
        //        {
        //            return (mBeatTimes[mBeatTimes.Count - 1] - mBeatTimes[0]) / (double)(BeatCount - 1);
        //        }            
        //    }
        //}

        public double ClosestFitBeatFrequency
        {
            get
            {
                double intercept;
                double slope;

                mLinearRegression.CalculateSlopeAndIntercept(out slope, out intercept);

                return slope;
            }

        }

        public double ClosestFitOffset
        {
            get
            {
                double intercept;
                double slope;

                mLinearRegression.CalculateSlopeAndIntercept(out slope, out intercept);

                return intercept;
            }
        }

        public double LastBeatFrequency
        {
            get
            {
                if (mBeatTimes.Count < 2)
                {
                    return 0;
                }
                else
                {
                    return mBeatTimes[mBeatTimes.Count - 1] - mBeatTimes[mBeatTimes.Count - 2];
                }
            }
        }

        public double LastBeatTime
        {
            get
            {
                if (mBeatTimes.Count == 0)
                {
                    return 0;
                }
                else
                {
                    return mBeatTimes[mBeatTimes.Count - 1];
                }
            }
        }

        //public double MinimumBeatSeparation
        //{
        //    get
        //    {
        //        if(mBeatTimes.Count < 2)
        //        {
        //            return 0;
        //        }

        //        double minValue = float.PositiveInfinity;

        //        for (int i = 1; i < mBeatTimes.Count; i++)
        //        {
        //            minValue = System.Math.Min(minValue, mBeatTimes[i] - mBeatTimes[i - 1]);
        //        }
        //        return minValue;
        //    }
        //}

        //public double MaximumBeatSeparation
        //{
        //    get
        //    {
        //        if (mBeatTimes.Count < 2)
        //        {
        //            return 0;
        //        }

        //        double maxValue = float.NegativeInfinity;

        //        for (int i = 1; i < mBeatTimes.Count; i++)
        //        {
        //            maxValue = System.Math.Max(maxValue, mBeatTimes[i] - mBeatTimes[i - 1]);
        //        }
        //        return maxValue;
        //    }
        //}

        //public double Offset
        //{
        //    get
        //    {
        //        if (mBeatTimes.Count == 0)
        //        {
        //            return 0;
        //        }
        //        else
        //        {
        //            return mBeatTimes[0];
        //        }
        //    }
        //}

        #endregion

        #region Methods

        public void Clear()
        {
            mBeatTimes.Clear();
        }


        public int GetNumberOfBeatsIntoSong(double timeIntoSong)
        {
            timeIntoSong -= ClosestFitOffset;

            return (int)((timeIntoSong) / ClosestFitBeatFrequency);
        }


        public void RecordBeat(double beatTime)
        {
            mBeatTimes.Add(beatTime);

            mLinearRegression.Points.Add(new FlatRedBall.Math.Geometry.Point(mLinearRegression.Points.Count, beatTime));
        }




        #endregion
    }
}
