using System;
namespace FlatRedBall.Glue.CodeGeneration.CodeBuilder
{
    public class CodeBlockSwitch : CodeBlockBase
    {
        public override bool IndentBody => true;
        public CodeBlockSwitch(ICodeBlock parent, string condition) : base(parent)
        {
            PreCodeLines.Add(new CodeLine("switch(" + condition + ")"));
            PreCodeLines.Add(new CodeLine("{"));
            PostCodeLines.Add(new CodeLine("}"));
        }
    }

    public class CodeBlockCase : CodeBlockBase
    {
        public override bool IndentBody => true;
        public CodeBlockCase(ICodeBlock parent, string condition)
            : base(parent)
        {
            PreCodeLines.Add(new CodeLine(StringHelper.SpaceStrings("case ",condition) + ":"));
            PostBodyCodeLines.Add(new CodeLine("break;"));
        }
    }

    public class CodeBlockCaseNoBreak : CodeBlockBase
    {
        public override bool IndentBody => true;
        public CodeBlockCaseNoBreak(ICodeBlock parent, string condition)
            : base(parent)
        {
            PreCodeLines.Add(new CodeLine(StringHelper.SpaceStrings("case ", condition) + ":"));
        }
    }

    public class CodeBlockDefault : CodeBlockBase
    {
        public override bool IndentBody => true;
        public CodeBlockDefault(ICodeBlock parent)
            : base(parent)
        {
            PreCodeLines.Add(new CodeLine("default:"));
            PostBodyCodeLines.Add(new CodeLine("break;"));
        }
    }

    public static class CodeBlockSwitchExtensions
    {
        public static ICodeBlock Switch(this ICodeBlock codeBlock, string condition)
        {
            if (codeBlock == null)
            {
                throw new NullReferenceException();
            }

            return new CodeBlockSwitch(codeBlock, condition);
        }

        public static ICodeBlock Case(this ICodeBlock codeBlock, string condition)
        {
            if (codeBlock == null)
            {
                throw new NullReferenceException();
            }

            return new CodeBlockCase(codeBlock, condition);
        }

        public static ICodeBlock CaseNoBreak(this ICodeBlock codeBlock, string condition)
        {
            if (codeBlock == null)
            {
                throw new NullReferenceException();
            }

            return new CodeBlockCaseNoBreak(codeBlock, condition);
        }

        public static ICodeBlock Default(this ICodeBlock codeBlock)
        {
            if (codeBlock == null)
            {
                throw new NullReferenceException();
            }

            return new CodeBlockDefault(codeBlock);
        }
    }
}
