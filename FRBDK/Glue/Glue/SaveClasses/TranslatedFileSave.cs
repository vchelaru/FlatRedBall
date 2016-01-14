using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.SaveClasses
{
    public enum TranslationStyle
    {
        PerformTranslate,
        IgnoreTranslation
    }

    public class TranslatedFileSave
    {
        public string FileName
        {
            get;
            set;
        }

        public TranslationStyle TranslationStyle
        {
            get;
            set;
        }
    }
}
