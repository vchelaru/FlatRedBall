// *
// * Copyright (C) 2008 Roger Alsing : http://www.RogerAlsing.com
// *
// * This library is free software; you can redistribute it and/or modify it
// * under the terms of the GNU Lesser General Public License 2.1 or later, as
// * published by the Free Software Foundation. See the included license.txt
// * or http://www.gnu.org/copyleft/lesser.html for details.
// *

namespace Alsing.SourceCode.SyntaxDocumentParsers
{
    public abstract class ParserBase : IParser
    {
        public SyntaxDocument Document { get; set; }
        public SyntaxDefinition SyntaxDefinition { get; set; }
        public string Separators { get; set; }

        public void Init(string syntaxDefinitionPath)
        {
            if (!syntaxDefinitionPath.ToLowerInvariant().EndsWith(".syn"))
                syntaxDefinitionPath += ".syn";

            var loader = new SyntaxDefinitionLoader();
            var syntax = loader.Load(syntaxDefinitionPath);
            Init(syntax);
        }

        public void Init(string syntaxDefinitionPath, string separators)
        {
            Separators = separators;
            Init(syntaxDefinitionPath);
        }

        public void Init(SyntaxDefinition syntaxDefinition)
        {
            SyntaxDefinition = syntaxDefinition;
        }

        public abstract void ParseRow(int rowIndex, bool parseKeywords);
        

        public void ParsePreviewLine(int rowIndex)
        {
            Row Row = Document[rowIndex];
            Row.Clear();
            Row.Add(Row.Text);
            Row.RowState = RowState.NotParsed;
        }
    }
}
