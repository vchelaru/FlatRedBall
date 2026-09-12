using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Glue.CodeGeneration.CodeBuilder
{
    public class CodeBlockTag : CodeBlockBaseNoIndent
    {
        public CodeBlockTag(ICodeBlock pParent, String pTagName) : base(pParent)
        {
            TagName = pTagName;
        }

        public string TagName { get; set; }

        public override List<ICodeBlock> GetTag(string pTagName)
        {
            return pTagName == TagName ? new List<ICodeBlock> { this } : null;
        }

        public override void AddToStringBuilder(StringBuilder pBldr, int pTabCount, string pTabCharacter)
        {
            if(PreCodeLines.Count == 0 && BodyCodeLines.Count == 0 && PostBodyCodeLines.Count == 0 && PostCodeLines.Count == 0)
            {
                var line = new CodeLine("// " + TagName + ":");
                line.AddToStringBuilder(pBldr, TabCount, TabCharacter);
            }else
            {
                base.AddToStringBuilder(pBldr, pTabCount, pTabCharacter);
            }
        }
    }

    public static class CodeBlockTagExtensions
    {
        public static ICodeBlock Tag(this ICodeBlock pCodeBlock, string pTagName)
        {
            return new CodeBlockTag(pCodeBlock, pTagName);
        }
    }
}
