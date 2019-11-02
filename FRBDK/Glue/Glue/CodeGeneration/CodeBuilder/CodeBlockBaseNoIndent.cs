using System.Text;

namespace FlatRedBall.Glue.CodeGeneration.CodeBuilder
{
    public class CodeBlockBaseNoIndent : CodeBlockBase
    {
        public CodeBlockBaseNoIndent()
            : base(null)
        {
        }

        public CodeBlockBaseNoIndent(ICodeBlock pParent)
            : base(pParent)
        {
        }

        public override void AddToStringBuilder(StringBuilder pBldr, int pTabCount, string pTabCharacter)
        {
            foreach (var codeLine in PreCodeLines)
            {
                codeLine.AddToStringBuilder(pBldr, pTabCount, pTabCharacter);
            }

            foreach (var codeLine in BodyCodeLines)
            {
                codeLine.AddToStringBuilder(pBldr, pTabCount, pTabCharacter);
            }

            foreach (var codeLine in PostBodyCodeLines)
            {
                codeLine.AddToStringBuilder(pBldr, pTabCount, pTabCharacter);
            }

            foreach (var codeLine in PostCodeLines)
            {
                codeLine.AddToStringBuilder(pBldr, pTabCount, pTabCharacter);
            }
        }
    }

    public class CodeDocument : CodeBlockBaseNoIndent
    {
        public CodeDocument() : base(null) { }

        public CodeDocument(int pTabCount, string pTabCharacter)
            : this()
        {
            TabCount = pTabCount;
            TabCharacter = pTabCharacter;
        }

        public CodeDocument(int pTabCount)
            : this()
        {
            TabCount = pTabCount;
        }

        public CodeDocument(string pTabCharacter)
            : this()
        {
            TabCharacter = pTabCharacter;
        }
    }

    public static class CodeBlockBaseNoIndentExtensions
    {
        public static ICodeBlock BlockWithNoIndent(this ICodeBlock pCodeBlock)
        {
            return new CodeBlockBaseNoIndent(pCodeBlock);
        }
    }
}
