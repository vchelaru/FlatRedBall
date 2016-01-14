using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using FlatRedBall.Instructions.Reflection;

namespace NonGraphicalTests.Instructions.Reflection
{
    public class ClassWithStringMember
    {
        public string StringMember;
    }

    [TestFixture]
    public class PropertyValuePairTests
    {
        [Test]
        public void TestCreatingShortHandCustomObjects()
        {
            ClassWithStringMember cwsm =
                PropertyValuePair.ConvertStringToType < ClassWithStringMember>("StringMember=\"Test With Space\"");

            if (cwsm.StringMember != "Test With Space")
            {
                throw new Exception("ConvertStringToType is not properly handling shorthand conversions using spaces");
            }


            cwsm =
                PropertyValuePair.ConvertStringToType<ClassWithStringMember>("StringMember  =  \"Test With Space\"");

            if (cwsm.StringMember != "Test With Space")
            {
                throw new Exception("ConvertStringToType is not properly handling shorthand conversions using spaces");
            }
        }

        [Test]
        public void TestStringCreation()
        {
            ClassWithStringMember cwsm =
                PropertyValuePair.ConvertStringToType<ClassWithStringMember>("StringMember=\"Test With, comma\"");

            if (cwsm.StringMember != "Test With, comma")
            {
                throw new Exception("ConvertStringToType is not properly handling commas");
            }

            // Just making sure this doesn't throw an exception.
            string result = PropertyValuePair.ConvertStringToType
                <string>("\"Beef = Yeah!\"");


        }


        [Test]
        public void TestStaticMemberReferences()
        {
            var result = PropertyValuePair.ConvertStringToType
                <Microsoft.Xna.Framework.Color>("Red");

            if (result != Microsoft.Xna.Framework.Color.Red)
            {
                throw new Exception("Color values are not using their static members properly");
            }

            result = PropertyValuePair.ConvertStringToType
                <Microsoft.Xna.Framework.Color>("R = 255");

            if (result.R != 255)
            {
                throw new Exception("Color values are not using their static members properly");
            }

            bool threwException = false;
            try
            {
                result = PropertyValuePair.ConvertStringToType
                    <Microsoft.Xna.Framework.Color>("Color.Red");
            }
            catch
            {
                threwException = true;
            }

            if (!threwException)
            {
                throw new Exception("The value \"Color.Red\" should throw an exception when trying to parse as Color.  " +
                    "Values should not prefix \"Color.\"");
            }
        }
    }
}
