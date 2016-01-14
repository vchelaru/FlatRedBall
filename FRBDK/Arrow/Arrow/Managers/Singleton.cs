using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Arrow.Managers
{
    public class Singleton<T> where T : new()
    {
        static T mSelf;

        public static T Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new T();
                }
                return mSelf;
            }
        }
    }
}
