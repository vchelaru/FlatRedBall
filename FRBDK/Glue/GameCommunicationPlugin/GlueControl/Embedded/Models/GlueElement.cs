using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using Newtonsoft.Json;
using FlatRedBall.IO;

namespace GlueControl.Models
{
    public abstract class GlueElement : IFileReferencer, INamedObjectContainer
    {

        public List<ReferencedFileSave> ReferencedFiles
        {
            get;
            set;
        }

        public bool UseGlobalContent
        {
            get;
            set;
        }

        public ReferencedFileSave GetReferencedFileSave(string fileName)
        {
            return IElementExtensionMethods.GetReferencedFileSave(this, fileName);
        }

        public bool IsOnOwnLayer
        {
            get;
            set;
        }

        [XmlIgnore]
        [JsonIgnore]
        public string ClassName
        {
            get
            {
                return FileManager.RemovePath(Name);
            }
        }

        public string Name
        {
            get;
            set;
        }

        public abstract string BaseElement { get; }

        [XmlIgnore]
        [JsonIgnore]
        public abstract string BaseObject
        {
            get; set;
        }

        [XmlIgnore]
        [JsonIgnore]
        public int VerificationIndex
        {
            get;
            set;
        }

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

        [XmlIgnore]
        [JsonIgnore]
        public override string BaseObject
        {
            get { return mBaseEntity; }
            set { mBaseEntity = value; }
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

        public override string BaseObject
        {
            get { return mBaseScreen; }
            set { mBaseScreen = value; }
        }

    }
}