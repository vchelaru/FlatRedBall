using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using FlatRedBall.Glue.Parsing;

namespace UnitTests
{






    [TestFixture]
    public class ParsedNamespaceTests
    {
        const string TestString =
@"
using BasementNeck.Screens;
namespace BasementNeck.Entities.GameEntities {
	public partial class ConfirmationDialog {

        void OnResumeButtonClickNoSlide (FlatRedBall.Gui.IWindow callingWindow)
        {
            
        }
        void OnExitButtonClickNoSlide (FlatRedBall.Gui.IWindow callingWindow)
        {
            
        }

	}
}
";

        const string TestStringClassOnly =
            @"
	public partial class ConfirmationDialog {

        void OnResumeButtonClickNoSlide (FlatRedBall.Gui.IWindow callingWindow)
        {
            
        }
        void OnExitButtonClickNoSlide (FlatRedBall.Gui.IWindow callingWindow)
        {
            
        }

	}

";

        [Test]
        public void Test()
        {

            ParsedClass parsedClass = new ParsedClass(TestStringClassOnly, false);
            if (parsedClass.ParsedMethods.Count < 2)
            {
                throw new Exception("ParsedMethod count is too low");
            }


            ParsedFile parsedFile = new ParsedFile();
            parsedFile.SetFromContents(TestString, false, false);

            if (parsedFile.Namespaces.Count == 0)
            {
                throw new Exception("ParsedFile namespace is 0 when it shouldn't be");
            }

            if (parsedFile.Namespaces[0].Classes.Count == 0)
            {
                throw new Exception("ParsedNamespace class count is 0 when it shouldn't be");
            }

            if (parsedFile.Namespaces[0].Classes[0].ParsedMethods.Count < 2)
            {
                throw new Exception("ParsedMethod count is too low");
            }

        }
    }
}
