using System.Collections.Generic;
using System.Text;

namespace FlatRedBall.Glue.CodeGeneration.CodeBuilder
{
    public interface ICode
    {
        string TabCharacter { get; set; }
        int TabCount { get; set; }

        void AddToStringBuilder(StringBuilder pBldr);
        void AddToStringBuilder(StringBuilder pBldr, int pTabCount, string pTabCharacter);
        string ToString();
        string ToString(int pTabCount, string pTabCharacter);
        List<ICodeBlock> GetTag(string pTagName);
        void Replace(string pOldValue, string pNewValue);
    }
}
