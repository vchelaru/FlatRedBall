// *
// * Copyright (C) 2008 Roger Alsing : http://www.RogerAlsing.com
// *
// * This library is free software; you can redistribute it and/or modify it
// * under the terms of the GNU Lesser General Public License 2.1 or later, as
// * published by the Free Software Foundation. See the included license.txt
// * or http://www.gnu.org/copyleft/lesser.html for details.
// *
// *

namespace Alsing.SourceCode
{
    //what kind of undoaction is it?
    /// <summary>
    /// 
    /// </summary>
    public enum UndoAction
    {
        /// <summary>
        /// 
        /// </summary>
        InsertRange = 1,
        /// <summary>
        /// 
        /// </summary>
        DeleteRange = 2,
    }

    //object that holds undo information
    /// <summary>
    /// 
    /// </summary>
    public sealed class UndoBlock
    {
        /// <summary>
        /// 
        /// </summary>
        public UndoAction Action = 0;

        /// <summary>
        /// 
        /// </summary>
        public TextPoint Position = new TextPoint(0, 0);

        /// <summary>
        /// 
        /// </summary>
        public string Text = "";
    }
}