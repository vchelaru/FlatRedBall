using System;
using System.Collections.Generic;
using System.Text;

namespace GlueControl.Models
{
    public class EntitySave : GlueElement
    {
        public override string BaseElement
        {
            get { return BaseEntity; }
        }

        string mBaseEntity;
        public string BaseEntity
        {
            get { return mBaseEntity; }
            set
            {
                if (value == "<NONE>")
                {
                    mBaseEntity = "";
                }
                else
                {
                    mBaseEntity = value;
                }

            }
        }
    }

    public abstract class GlueElement
    {
        public string Name
        {
            get;
            set;
        }

        public abstract string BaseElement { get; }

    }
}