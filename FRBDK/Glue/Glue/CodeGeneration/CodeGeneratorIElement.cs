using System;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Parsing;
using System.Threading.Tasks;

namespace FlatRedBall.Glue.CodeGeneration
{
    // FRB1-only: FRB2 has its own IGenerateCodeCommands implementation (Frb2GenerateCodeCommands)
    // that calls Frb2CodeGenerator directly rather than through here - see GlueCommands.GenerateCodeCommands.
    public class CodeGeneratorIElement
    {
        public static async Task GenerateSpecificElement(GlueElement element)
        {
#if DEBUG
            if(element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }
#endif

            await CodeWriter.GenerateCode(element);
        }

        public static async Task GenerateElementAndDerivedCode(GlueElement baseElement)
        {
            await GenerateSpecificElement(baseElement);

            var derivedElements = ObjectFinder.Self.GetAllElementsThatInheritFrom(baseElement);

            foreach (var element in derivedElements)
            {
                await GenerateSpecificElement(element);
            }
        }




    }
}
