using System;

namespace Alsing.SourceCode
{
    /// <summary>
    /// 
    /// </summary>
    public class RowEventArgs : EventArgs
    {
        /// <summary>
        /// 
        /// </summary>
        public Row Row;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="row"></param>
        public RowEventArgs(Row row)
        {
            Row = row;
        }
    }
}
