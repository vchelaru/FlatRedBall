using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace FlatRedBall.Glue.CodeGeneration.CodeBuilder
{
    public class CodeBlockBase : ICodeBlock
    {
        private readonly List<ICode> _preCodeLines = new List<ICode>();
        private readonly List<ICode> _bodyCodeLines = new List<ICode>();
        private readonly List<ICode> _postCodeLines = new List<ICode>();
        private readonly List<ICode> _postBodyCodeLines = new List<ICode>();

        public virtual bool IndentBody { get; set; } = false;

        public CodeBlockBase(ICodeBlock parent = null)
        {
            TabCharacter = CodeBuilderDefaults.TabCharacter;
            TabCount = CodeBuilderDefaults.TabCount;
            Parent = parent;
            if(Parent != null)
                Parent.BodyCodeLines.Add(this);
        }

        public string TabCharacter { get; set; }
        public int TabCount { get; set; }
        public ICodeBlock Parent { get; set; }
        public List<ICode> PreCodeLines
        {
            get { return _preCodeLines; }
        }
        public List<ICode> BodyCodeLines
        {
            get { return _bodyCodeLines; }
        }
        public List<ICode> PostBodyCodeLines
        {
            get { return _postBodyCodeLines; }
        }
        public List<ICode> PostCodeLines
        {
            get { return _postCodeLines; }
        }


        public void AddToStringBuilder(StringBuilder builder)
        {
            AddToStringBuilder(builder, TabCount, TabCharacter);
        }

        public virtual void AddToStringBuilder(StringBuilder builder, int tabCount, string tabCharacter)
        {
            foreach (var codeLine in PreCodeLines)
            {
                codeLine.AddToStringBuilder(builder, tabCount, tabCharacter);
            }

            if(IndentBody)
            {
                tabCount++;
            }

            foreach (var codeLine in BodyCodeLines)
            {
                codeLine.AddToStringBuilder(builder, tabCount, tabCharacter);
            }

            foreach (var codeLine in PostBodyCodeLines)
            {
                codeLine.AddToStringBuilder(builder, tabCount, tabCharacter);
            }

            if(IndentBody)
            {
                tabCount--;
            }

            foreach (var codeLine in PostCodeLines)
            {
                codeLine.AddToStringBuilder(builder, tabCount, tabCharacter);
            }
        }

        public override string ToString()
        {
            return ToString(TabCount, TabCharacter);
        }

        public string ToString(int tabCount, string tabCharacter)
        {
            var bldr = new StringBuilder();

            AddToStringBuilder(bldr, tabCount, tabCharacter);

            return bldr.ToString();
        }

        public virtual List<ICodeBlock> GetTag(string tagName)
        {
            var returnList = new List<ICodeBlock>();

            foreach (var line in _preCodeLines)
            {
                var list = line.GetTag(tagName);
                if(list != null)
                    returnList.AddRange(list);
            }

            foreach (var line in _bodyCodeLines)
            {
                var list = line.GetTag(tagName);
                if (list != null)
                    returnList.AddRange(list);
            }

            foreach (var line in _postCodeLines)
            {
                var list = line.GetTag(tagName);
                if (list != null)
                    returnList.AddRange(list);
            }

            foreach (var line in _postBodyCodeLines)
            {
                var list = line.GetTag(tagName);
                if (list != null)
                    returnList.AddRange(list);
            }

            return returnList;
        }

        public void Replace(string oldValue, string newValue)
        {
            foreach (var line in PreCodeLines)
            {
                line.Replace(oldValue, newValue);
            }

            foreach (var line in BodyCodeLines)
            {
                line.Replace(oldValue, newValue);
            }

            foreach (var line in PostBodyCodeLines)
            {
                line.Replace(oldValue, newValue);
            }

            foreach (var line in PostCodeLines)
            {
                line.Replace(oldValue, newValue);
            }
        }
    }

    public static class CodeBlockExtensions
    {
        /// <summary>
        /// Creates an indented code block without surrounding brackes - only indents. For brackets, use Block.
        /// </summary>
        /// <param name="codeBlock"></param>
        /// <returns></returns>
        public static ICodeBlock CodeBlockIndented(this ICodeBlock codeBlock)
        {
            var block = new CodeBlockBase(codeBlock);
            block.IndentBody = true;
            return block;
        }

        public static ICodeBlock End(this ICodeBlock codeBlock)
        {
            return codeBlock.Parent;
        }

        public static string CleanUpSpaces(this string value)
        {
            return Regex.Replace(value, @"\s+", " ").Trim();
        }
    }
}
