using System;
using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Glue.CodeGeneration.CodeBuilder
{
    public class CodeLine : ICode
    {
        private string _value;

        public string Value => _value;

        public CodeLine(string value)
        {
            _value = value;

            if (String.IsNullOrEmpty(_value)) _value = "";
        }

        public string TabCharacter { get; set; }

        public int TabCount { get; set; }

        public void AddToStringBuilder(StringBuilder pBldr)
        {
            AddToStringBuilder(pBldr, TabCount, TabCharacter);
        }

        public void AddToStringBuilder(StringBuilder pBldr, int pTabCount, string pTabCharacter)
        {
            for (var i = 0; i < pTabCount; i++)
            {
                pBldr.Append(pTabCharacter);
            }

            pBldr.AppendLine(_value);
        }

        public override string ToString()
        {
            return ToString(TabCount, TabCharacter);
        }

        public string ToString(int pTabCount, string pTabCharacter)
        {
            var bldr = new StringBuilder();
            
            AddToStringBuilder(bldr, pTabCount, pTabCharacter);

            return bldr.ToString();
        }

        public List<ICodeBlock> GetTag(string pTagName)
        {
            return null;
        }

        public void Replace(string pOldValue, string pNewValue)
        {
            _value = _value.Replace(pOldValue, pNewValue);
        }
    }

    public static class CodeLineExtensionMethods
    {
        public static ICodeBlock Line(this ICodeBlock pCodeBlock, string value)
        {
            var returnValue = new CodeLine(value);

            pCodeBlock.BodyCodeLines.Add(returnValue);

            return pCodeBlock;
        }

        /// <summary>
        /// Adds value as a line of code
        /// </summary>
        /// <param name="pCodeBlock">Parent block to add to</param>
        /// <param name="value">Code to add</param>
        /// <returns>Parent block</returns>
        public static ICodeBlock _(this ICodeBlock pCodeBlock, string value)
        {
            var returnValue = new CodeLine(value);

            pCodeBlock.BodyCodeLines.Add(returnValue);

            return pCodeBlock;
        }

        /// <summary>
        /// Places a blank line in the generated code.
        /// </summary>
        /// <param name="pCodeBlock">Block to add line to.</param>
        /// <returns>Parent block</returns>
        public static ICodeBlock _(this ICodeBlock pCodeBlock)
        {
            var returnValue = new CodeLine("");

            pCodeBlock.BodyCodeLines.Add(returnValue);

            return pCodeBlock;
        }

        public static bool HasLine(this ICodeBlock pCodeBlock, string value)
        {
            foreach(var line in pCodeBlock.BodyCodeLines)
            {
                if(line is CodeLine && ((CodeLine)line).Value == value)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
