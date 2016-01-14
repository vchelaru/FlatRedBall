using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Graphics.Particle
{
    public struct TimedRemovalRecord
    {
        public Sprite SpriteToRemove;
        public double TimeToRemove;
    }



    public static class TimedRemovalRecordListExtensionMethods
    {
        public static void InsertSorted(this List<TimedRemovalRecord> list, Sprite sprite, double timeToRemove)
        {

            TimedRemovalRecord record = new TimedRemovalRecord();
            record.SpriteToRemove = sprite;
            record.TimeToRemove = timeToRemove;

            int index = GetFirstAfterTime(list, record.TimeToRemove);
            list.Insert(index, record);

        }

        public static void InsertSorted(this List<TimedRemovalRecord> list, TimedRemovalRecord record)
        {
            int index = GetFirstAfterTime(list, record.TimeToRemove);
            list.Insert(index, record);
        }

        public static int GetFirstAfterTime(List<TimedRemovalRecord> list, double value)
        {

            if (list.Count == 0)
            {
                return 0;
            }
            int lowBound = 0;
            int highBound = list.Count - 1;

            int current;


            while (true)
            {
                current = (lowBound + highBound) >> 1;
                if (highBound - lowBound < 2)
                {
                    if (list[highBound].TimeToRemove <= value)
                    {
                        return highBound + 1;
                    }
                    else if (list[lowBound].TimeToRemove <= value)
                    {
                        return lowBound + 1;
                    }
                    else if (list[lowBound].TimeToRemove > value)
                    {
                        return lowBound;
                    }
                }

                if (list[current].TimeToRemove >= value)
                {
                    highBound = current;
                }
                else if (list[current].TimeToRemove < value)
                {
                    lowBound = current;
                }
            }

            // Unreachable code:
            //return 0;

        }
    }
}
