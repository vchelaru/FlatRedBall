// *
// * Copyright (C) 2008 Roger Alsing : http://www.RogerAlsing.com
// *
// * This library is free software; you can redistribute it and/or modify it
// * under the terms of the GNU Lesser General Public License 2.1 or later, as
// * published by the Free Software Foundation. See the included license.txt
// * or http://www.gnu.org/copyleft/lesser.html for details.
// *
// *

namespace Alsing.SourceCode.SyntaxDocumentParsers
{
    /// <summary>
    /// Parser interface.
    /// Implement this interface if you want to create your own parser.
    /// </summary>
    public interface IParser
    {
        /// <summary>
        /// Gets or Sets the Document object for this parser
        /// </summary>
        SyntaxDocument Document { get; set; }

        /// <summary>
        /// Gets or Sets the SyntaxDefinition for this parser
        /// </summary>
        SyntaxDefinition SyntaxDefinition { get; set; }

        string Separators { get; set; }

        /// <summary>
        /// Initializes the parser with a spcified SyntaxFile
        /// </summary>
        /// <param name="syntaxDefinitionPath">Filename of the SyntaxFile that should be used</param>
        void Init(string syntaxDefinitionPath);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="syntaxDefinitionPath"></param>
        /// <param name="separators"></param>
        void Init(string syntaxDefinitionPath, string separators);

        /// <summary>
        /// Initializes the parser with a spcified syntaxDefinition object
        /// </summary>
        /// <param name="syntaxDefinition">The Language object to assign to the parser</param>
        void Init(SyntaxDefinition syntaxDefinition);

        /// <summary>
        /// Called by the SyntaxDocument object when a row should be parsed
        /// </summary>
        /// <param name="RowIndex">The row index in the document</param>
        /// <param name="ParseKeywords">true if keywords and operators should be parsed , false if only a span parse should be performed</param>
        void ParseRow(int RowIndex, bool ParseKeywords);

        /// <summary>
        /// Called by the SyntaxDocument object when a row must be preview parsed.
        /// </summary>
        /// <param name="RowIndex">Row index in the document</param>
        void ParsePreviewLine(int RowIndex);
    }
}