// *
// * Copyright (C) 2008 Roger Alsing : http://www.RogerAlsing.com
// *
// * This library is free software; you can redistribute it and/or modify it
// * under the terms of the GNU Lesser General Public License 2.1 or later, as
// * published by the Free Software Foundation. See the included license.txt
// * or http://www.gnu.org/copyleft/lesser.html for details.
// *
// *

using System.Collections.Generic;

namespace Alsing.SourceCode
{
    /// <summary>
    /// File type struct
    /// </summary>
    public class FileType
    {
        /// <summary>
        /// The file type extension
        /// </summary>
        public string Extension = "";

        /// <summary>
        /// The name of the file type
        /// </summary>
        public string Name = "";
    }

    /// <summary>
    /// The SyntaxDefinition class describes a syntax.<br/>
    /// It consists of a mainSpanDefinition , which is the start spanDefinition of the SyntaxDefinition<br/>
    /// It also have a list of filetypes that is valid for this syntax<br/>
    /// </summary>
    /// <example>
    /// <b>Apply a Syntax to a SyntaxBox</b>
    /// <code>
    /// SyntaxBoxControl1.Document.SyntaxFile="C#.syn";
    /// </code>
    /// </example>
    public class SyntaxDefinition
    {
        #region PUBLIC PROPERTY SEPARATORS

        private string _Separators = ".,:;{}()[]+-*/\\ \t=&%$#@!|&";

        public string Separators
        {
            get { return _Separators; }
            set { _Separators = value; }
        }

        #endregion

        #region PUBLIC PROPERTY VERSION

        private long _Version = long.MinValue;

        public long Version
        {
            get { return _Version; }
            set { _Version = value; }
        }

        #endregion

        private readonly Dictionary<SpanDefinition, SpanDefinition> spanDefinitionLookup = new Dictionary<SpanDefinition, SpanDefinition>();
        private readonly Dictionary<TextStyle, TextStyle> styleLookup = new Dictionary<TextStyle, TextStyle>();

        /// <summary>
        /// List containing the valid filetypes for this syntax
        /// </summary>
        public List<FileType> FileTypes = new List<FileType>();

        /// <summary>
        /// The start spanDefinition for this syntax
        /// </summary>
        public SpanDefinition mainSpanDefinition;

        /// <summary>
        /// Name of the SyntaxDefinition
        /// </summary>
        public string Name = "";

        /// <summary>
        /// Gets all BlockTypes in a given syntax.
        /// </summary>
        public SpanDefinition[] SpanDefinitions
        {
            get
            {
                spanDefinitionLookup.Clear();
                FillBlocks(mainSpanDefinition);
                var blocks = new SpanDefinition[spanDefinitionLookup.Values.Count];
                int i = 0;
                foreach (SpanDefinition bt in spanDefinitionLookup.Values)
                {
                    blocks[i] = bt;
                    i++;
                }

                return blocks;
            }
        }

        public TextStyle[] Styles
        {
            get
            {
                styleLookup.Clear();
                SpanDefinition[] spanDefinitions = SpanDefinitions;
                foreach (SpanDefinition bt in spanDefinitions)
                {
                    styleLookup[bt.Style] = bt.Style;

                    foreach (Scope sc in bt.ScopePatterns)
                    {
                        if (sc.Style != null)
                            styleLookup[sc.Style] = sc.Style;
                    }

                    foreach (PatternList pl in bt.KeywordsList)
                    {
                        if (pl.Style != null)
                            styleLookup[pl.Style] = pl.Style;
                    }

                    foreach (PatternList pl in bt.OperatorsList)
                    {
                        if (pl.Style != null)
                            styleLookup[pl.Style] = pl.Style;
                    }
                }

                var styles = new TextStyle[styleLookup.Values.Count];
                int i = 0;
                foreach (TextStyle st in styleLookup.Values)
                {
                    styles[i] = st;
                    i++;
                }
                return styles;
            }
        }

        public void UpdateLists()
        {
            SpanDefinition[] spanDefinitions = SpanDefinitions;
            foreach (SpanDefinition block in spanDefinitions)
            {
                block.Parent = this;
                block.ResetLookupTable();

                block.KeywordsList.Parent = block;
                foreach (PatternList patterns in block.KeywordsList)
                {
                    patterns.Parent = block.KeywordsList;

                    foreach (Pattern pattern in patterns)
                    {
                        block.AddToLookupTable(pattern);
                    }
                }

                block.OperatorsList.Parent = block;
                foreach (PatternList patterns in block.OperatorsList)
                {
                    patterns.Parent = block.OperatorsList;

                    foreach (Pattern pattern in patterns)
                    {
                        block.AddToLookupTable(pattern);
                    }
                }
                block.BuildLookupTable();
            }
        }

        public void ChangeVersion()
        {
            Version++;
            if (Version > long.MaxValue - 10)
                Version = long.MinValue;
        }

        public static SyntaxDefinition FromSyntaxXml(string xml)
        {
            var sl = new SyntaxDefinitionLoader();
            return sl.LoadXML(xml);
        }

        public static SyntaxDefinition FromSyntaxFile(string filename)
        {
            var sl = new SyntaxDefinitionLoader();
            return sl.Load(filename);
        }

        public void MergeByMainBlock(SyntaxDefinition Target)
        {
            SpanDefinition[] spanDefinitions = SpanDefinitions;
            foreach (SpanDefinition bt in spanDefinitions)
            {
                bt.childSpanDefinitions.Insert(0, Target.mainSpanDefinition);
            }
        }

        public void MergeByChildBlocks(SyntaxDefinition Target)
        {
            SpanDefinition[] spanDefinitions = SpanDefinitions;
            foreach (SpanDefinition bt in spanDefinitions)
            {
                for (int i = Target.mainSpanDefinition.childSpanDefinitions.Count - 1; i >= 0; i--)
                {
                    SpanDefinition child = Target.mainSpanDefinition.childSpanDefinitions[i];
                    bt.childSpanDefinitions.Insert(0, child);
                }
            }
        }


        private void FillBlocks(SpanDefinition bt)
        {
            if (bt == null)
                return;

            if (spanDefinitionLookup.ContainsKey(bt))
                return;

            spanDefinitionLookup.Add(bt, bt);

            foreach (SpanDefinition btc in bt.childSpanDefinitions)
            {
                FillBlocks(btc);
            }
            foreach (Scope sc in bt.ScopePatterns)
            {
                FillBlocks(sc.spawnSpanOnEnd);
                FillBlocks(sc.spawnSpanOnStart);
            }
        }
    }
}