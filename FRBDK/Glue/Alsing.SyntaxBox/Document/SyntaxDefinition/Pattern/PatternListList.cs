// *
// * Copyright (C) 2008 Roger Alsing : http://www.RogerAlsing.com
// *
// * This library is free software; you can redistribute it and/or modify it
// * under the terms of the GNU Lesser General Public License 2.1 or later, as
// * published by the Free Software Foundation. See the included license.txt
// * or http://www.gnu.org/copyleft/lesser.html for details.
// *
// *

using System.Collections;

namespace Alsing.SourceCode
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class PatternListList : IEnumerable
    {
        private readonly ArrayList mGroups = new ArrayList();

        /// <summary>
        /// 
        /// </summary>
        public bool IsKeyword;

        /// <summary>
        /// 
        /// </summary>
        public bool IsOperator;

        /// <summary>
        /// 
        /// </summary>
        public SpanDefinition Parent;

        /// <summary>
        /// 
        /// </summary>
        public PatternListList() {}

        public PatternListList(SpanDefinition parent)
        {
            Parent = parent;
        }

        /// <summary>
        /// 
        /// </summary>
        public PatternList this[int index]
        {
            get { return (PatternList) mGroups[index]; }

            set { mGroups[index] = value; }
        }

        #region IEnumerable Members

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerator GetEnumerator()
        {
            return mGroups.GetEnumerator();
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Group"></param>
        /// <returns></returns>
        public PatternList Add(PatternList Group)
        {
            mGroups.Add(Group);
            Group.Parent = this;
            if (Parent != null && Parent.Parent != null)
                Parent.Parent.ChangeVersion();

            return Group;
        }

        /// <summary>
        /// 
        /// </summary>
        public void Clear()
        {
            mGroups.Clear();
        }
    }
}