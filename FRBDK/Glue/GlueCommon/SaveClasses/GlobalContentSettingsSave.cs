using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.SaveClasses
{
    public class GlobalContentSettingsSave
    {
        public bool LoadAsynchronously
        {
            get;
            set;
        }

        public bool RecordLockContention
        {
            get;
            set;
        }

        public bool GenerateLoadGlobalContentCode
        {
            get; set;
        } = true;

        /// <summary>
        /// When true, newly created entities have UseGlobalContent set to true. Existing entities are not changed.
        /// </summary>
        public bool DefaultNewEntitiesToUseGlobalContent
        {
            get; set;
        }
    }
}
