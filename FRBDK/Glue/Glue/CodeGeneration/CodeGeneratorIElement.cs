using System;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Parsing;

namespace FlatRedBall.Glue.CodeGeneration
{
    public class CodeGeneratorIElement
    {
        public static void GenerateElement(IElement element)
        {
#if DEBUG
            if(element == null)
            {
                throw new ArgumentNullException(nameof(element));
            }
#endif

            CodeWriter.GenerateCode(element);
        }

        public static void GenerateElementAndDerivedCode(IElement baseElement)
        {
            GenerateElement(baseElement);

            var derivedElements = ObjectFinder.Self.GetAllElementsThatInheritFrom(baseElement);

            foreach (var element in derivedElements)
            {
                GenerateElement(element);
            }
        }

        public static void GenerateElementDerivedAndReferenced(IElement baseElement)
        {
            if (baseElement != null)
            {
                GenerateElementAndDerivedCode(baseElement);

                var namedObjects = ObjectFinder.Self.GetAllNamedObjectsThatUseElement(baseElement);

                foreach (var nos in namedObjects)
                {
                    IElement element = ObjectFinder.Self.GetElementContaining(nos);
                    GenerateElement(element);
                }
            }
        }


    }
}
