using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GlueViewUnitTests;

namespace ArrowUnitTests
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                TestFramework.RunTests();
            }
            catch (Exception e)
            {
                Console.Write(e.InnerException.ToString());
            }
        }
    }
}
