using System;
using System.Collections.Generic;
using System.Text;

namespace GlueControl.Models
{
    public abstract class GlueElement
    {
        public bool IsOnOwnLayer
        {
            get;
            set;
        }


        public string Name
        {
            get;
            set;
        }

        public abstract string BaseElement { get; }

        public IEnumerable<NamedObjectSave> AllNamedObjects
        {
            get
            {
                foreach (NamedObjectSave nos in NamedObjects)
                {
                    yield return nos;

                    foreach (NamedObjectSave containedNos in nos.ContainedObjects)
                    {
                        yield return containedNos;
                    }
                }
            }
        }


        public List<NamedObjectSave> NamedObjects
        {
            get;
            set;
        } = new List<NamedObjectSave>();
    }

    public class EntitySave : GlueElement
    {
        public override string BaseElement => BaseEntity; 

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

    public class ScreenSave : GlueElement
    {
        string mBaseScreen;

        public string BaseScreen
        {
            get { return mBaseScreen; }
            set
            {
                if (value == "<NONE>")
                {
                    mBaseScreen = "";
                }
                else
                {
                    mBaseScreen = value;
                }
            }
        }

        public override string BaseElement => BaseScreen;

    }
}