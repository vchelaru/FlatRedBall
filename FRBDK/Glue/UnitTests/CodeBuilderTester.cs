using System.Diagnostics;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    class CodeBuilderTester
    {
        [Test]
        public void TestCode()
        {
            var codeFile = new CodeDocument {TabCharacter = "  "};

            codeFile.Namespace("Test")
                .Class("public", "TestClass", "")
                    ._()
                    ._("private string _value;")
                    ._()
                    .Function("public", "TestClass", "string value")
                        ._("_value = value;")
                    .End()
                    ._()
                    .Function("public void ", "TestMethod", "")
                        ._("_value = 5;")
                    .End()
                    ._()
                    ._()
                    .Interface("public", "TestInterface", "")
                        ._("void TestMethod();")
                    .End()
                .End()
            .End();

            Debug.WriteLine(codeFile.ToString());

            Assert.IsTrue(true);
        }
    }
}
