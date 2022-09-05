using FlatRedBall.Glue.Events;
using FlatRedBall.IO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FlatRedBall.Glue.SaveClasses
{
    /// <summary>
    /// Base class for Glue Screens and Entities.
    /// </summary>
    /// <remarks>
    /// This provides a place for common code for Screens and Entities without using 
    /// interface default implementations (which introduce their own problems). Vic believes
    /// that this should be the default type passed around rather than IElement going forward.
    /// </remarks>
    public abstract class GlueElement : IFileReferencer, IElement, ITaggable, INamedObjectContainer
    {
        List<string> ITaggable.Tags => this.Tags;

        string ITaggable.Source => this.Source;

        public List<string> Tags = new List<string>();
        public string Source = "";

        [CategoryAttribute("Performance")]
        public bool UseGlobalContent
        {
            get;
            set;
        }

        public ReferencedFileSave GetReferencedFileSave(string fileName)
        {
            return FileReferencerHelper.GetReferencedFileSave(this, fileName);
        }

        [Browsable(false)]
        public List<ReferencedFileSave> ReferencedFiles
        {
            get;
            set;
        }
        public bool ShouldSerializeReferencedFiles()
        {
            return ReferencedFiles != null && ReferencedFiles.Count != 0;
        }

        /// <summary>
        /// Whether this element should be hidden in the Glue tree view.
        /// This is false for most elements, but will be true for elements
        /// which are managed automatically by a plugin, such as levels from TMX
        /// </summary>
        public bool IsHiddenInTreeView
        {
            get; set;
        }

        [Browsable(false)]
        public List<CustomVariable> CustomVariables
        {
            get;
            set;
        }
        public bool ShouldSerializeCustomVariables()
        {
            return CustomVariables != null && CustomVariables.Count != 0;
        }

        [Browsable(false)]
        [XmlIgnore]
        [JsonIgnore]
        public bool HasChanged { get; set; }

        [Browsable(false)]
        [XmlIgnore]
        [JsonIgnore]
        public IEnumerable<StateSave> AllStates
        {
            get
            {
                foreach (StateSave state in States)
                {
                    yield return state;
                }
                foreach (StateSaveCategory category in this.StateCategoryList)
                {
                    foreach (StateSave state in category.States)
                    {
                        yield return state;
                    }
                }
            }
        }

        [Browsable(false)]
        public List<StateSave> States
        {
            get;
            set;
        }
        public bool ShouldSerializeStates()
        {
            return States != null && States.Count != 0;
        }

        public List<PropertySave> Properties
        {
            get;
            set;
        } = new List<PropertySave>();
        public bool ShouldSerializeProperties()
        {
            return Properties != null && Properties.Count != 0;
        }

        /// <summary>
        /// A flattened IEnumerable of all named objects stored on this element. This includes top-level named objects and
        /// named objects contained in lists. This does not return objects from base objects unless they also appear in this
        /// object list.
        /// </summary>
        [Browsable(false)]
        [XmlIgnore]
        [JsonIgnore]
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


        [Browsable(false)]
        public List<NamedObjectSave> NamedObjects
        {
            get;
            set;
        }
        public bool ShouldSerializeNamedObjects()
        {
            return NamedObjects != null && NamedObjects.Count != 0;
        }


        [Browsable(false)]
        public List<StateSaveCategory> StateCategoryList
        {
            get;
            set;
        }
        public bool ShouldSerializeStateCategoryList()
        {
            return StateCategoryList != null && StateCategoryList.Count != 0;
        }


        [Browsable(false)]
        [XmlIgnore]
        [JsonIgnore]
        public bool HasStates
        {
            get
            {
                if (States.Count != 0)
                {
                    return true;
                }

                foreach (StateSaveCategory category in StateCategoryList)
                {
                    if (category.States.Count != 0)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        [XmlIgnore]
        [JsonIgnore]
        [DisplayName("Name")]
        public string ClassName
        {
            get
            {
                return FileManager.RemovePath(Name);
            }
        }

        [Browsable(false)]
        public string Name
        {
            get;
            set;
        }

        public abstract string BaseElement { get; }

        [XmlIgnore]
        [JsonIgnore]
        [Browsable(false)]
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


        [Browsable(false)]
        public List<EventResponseSave> Events
        {
            get;
            set;
        }
        public bool ShouldSerializeEvents()
        {
            return Events != null && Events.Count != 0;
        }

        public EventResponseSave GetEvent(string eventName)
        {
            foreach (EventResponseSave es in Events)
            {
                if (es.EventName == eventName)
                {
                    return es;
                }
            }

            return null;
        }

        /// <summary>
        /// The custom classes contained in the project when the export occurred.
        /// This is necessary because some CSVs use custom classes
        /// </summary>
        public List<CustomClassSave> CustomClassesForExport
        {
            get; set;
        } = new List<CustomClassSave>();

        public CustomVariable GetCustomVariable(string customVariableName)
        {
            foreach (var customVariable in CustomVariables)
            {
                if (customVariable.Name == customVariableName)
                {
                    return customVariable;
                }
            }
            return null;
        }


        public object GetPropertyValue(string propertyName)
        {
            return Properties.GetValue(propertyName);
        }

        public bool IsAbstract => this.AllNamedObjects.Any(item => item.SetByDerived);
    }
}
