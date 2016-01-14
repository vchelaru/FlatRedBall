using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Math
{
    public class NaNException : Exception
    {
        public string MemberName
        {
            get;
            set;
        }

        

        public NaNException()
            : base()
        {
            


        }

        public NaNException(string message, string memberName)
            : base()
        {
            MemberName = memberName;
        }



    }
}
