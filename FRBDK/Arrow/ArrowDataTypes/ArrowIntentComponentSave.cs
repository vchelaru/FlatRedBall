using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Arrow.DataTypes
{
    #region Enums
    public enum CharacteristicRequirement
    {
        Undefined,
        MustBe,
        MustNotBe
    }

    public enum GlueItemType
    {
        Undefined,
        Screen,
        Entity,
        File,
        NamedObject
    }

    #endregion

    public class ArrowIntentComponentSave
    {
        public string RequiredName
        {
            get;
            set;
        }

        public GlueItemType GlueItemType
        {
            get;
            set;
        }

        public CharacteristicRequirement IsFileRequirement
        {
            get;
            set;
        }

        public string RequiredExtension
        {
            get;
            set;
        }

        public bool LoadedOnlyWhenReferenced
        {
            get;
            set;
        }


    }
}
