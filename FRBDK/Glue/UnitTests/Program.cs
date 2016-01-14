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
            bool succeeded = false;
            try
            {

                TestFramework.RunTests();
                succeeded = true;
            }

            catch (Exception e)
            {
                succeeded = false;
                WriteErrorRecursively(e);


            }


            if (succeeded)
            {
                System.Console.WriteLine("All tests passed!");
            }

            System.Console.ReadLine();
        }

        private static void WriteErrorRecursively(Exception e, string prefix = "Error:\n")
        {
            System.Console.WriteLine(prefix + e.ToString());

            if (e.InnerException != null)
            {
                WriteErrorRecursively(e.InnerException);
            }
        }


    }
}
