using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GlueTestProject.RuntimeTypes;

namespace GlueTestProject.DataTypes
{
    public class TestSaveClass
    {
        public static TestSaveClass FromFile(string fileName)
        {
            TestSaveClass tsc = new TestSaveClass();

            return tsc;
        }

        public TestRuntimeClass ToRuntime()
        {
            TestRuntimeClass trc = new TestRuntimeClass();

            return trc;
        }



    }
}
