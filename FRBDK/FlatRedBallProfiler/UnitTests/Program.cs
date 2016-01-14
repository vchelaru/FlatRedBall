using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GlueViewUnitTests;

namespace UnitTests
{
    class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                TestFramework.RunTests();
            }
            catch (Exception e)
            {
                System.Console.Write(e.ToString());


            }


        }
    }
}
