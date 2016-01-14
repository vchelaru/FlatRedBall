using System;
using GlueViewUnitTests.ScriptParsing;

namespace GlueViewUnitTests
{
    class Program
    {
        public static void Main(string[] args)
        {
            bool passed = false;


            bool runOrdered = false;
            try
            {
                if (runOrdered)
                {
                    RunOrdered();
                }
                else
                {
                    TestFramework.RunTests();

                }
                    passed = true;
            }
            catch (Exception e)
            {
                System.Console.Write(e);
                passed = false;
            }


            if (passed)
            {
                System.Console.WriteLine("All tests passed");
            }

            System.Console.ReadLine();

        }

        private static void RunOrdered()
        {
            ExpressionParserTests ept = new ExpressionParserTests();
            ept.Initialize();
            ept.TestCodeParser();
            ept.TestEvaluation();
            ept.TestInterpolateBetween();
            ept.TestMultipleLines();
            ept.TestParsedLine();
            ept.TestStates();
        }


    }
}
