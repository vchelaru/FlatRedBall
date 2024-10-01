using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.GuiDisplay;
using FlatRedBall.Glue.Events;
using FlatRedBall.Glue.FormHelpers.StringConverters;
using FlatRedBall.Glue.SaveClasses;

namespace FlatRedBall.Glue.FormHelpers.PropertyGrids
{
    public class EventResponseSavePropertyGridDisplayer : PropertyGridDisplayer
    {
        #region Properties

        public GlueElement CurrentElement
        {
            get;
            set;
        }

        public override object Instance
        {
            get
            {
                return base.Instance;
            }
            set
            {
                // mInstance needs to be set, but
                // we don't want to do it through the
                // Instane property because that will trigger 
                // code that shouldn't be triggered until after
                // UpdateIncludeAndExclude
                mInstance = value;
                UpdateIncludedAndExcluded(value as EventResponseSave);

                base.Instance = value;
            }
        }

        #endregion

        private void UpdateIncludedAndExcluded(EventResponseSave instance)
        {
            ////////////////////Early Out/////////////////////////
            if (instance == null)
            {
                return;
            }
            ///////////////////End Early Out///////////////////////
            ResetToDefault();

            ExcludeMember("ToStringDelegate");
            ExcludeMember("Contents");

            AvailableCustomVariables typeConverter = new AvailableCustomVariables(CurrentElement);
            typeConverter.IncludeNone = false;

            typeConverter.InclusionPredicate = DoesCustomVariableCreateEvent;
            IncludeMember(typeof(EventResponseSave).GetProperty("SourceVariable").Name,
                typeof(EventResponseSave), typeConverter);


            if (string.IsNullOrEmpty(instance.SourceVariable))
            {
                ExcludeMember( typeof(EventResponseSave).GetProperty("BeforeOrAfter").Name);
            }



            AvailableNamedObjectsAndFiles availableNamedObjects = new AvailableNamedObjectsAndFiles(
                CurrentElement);
            availableNamedObjects.IncludeReferencedFiles = false;
            IncludeMember(typeof(EventResponseSave).GetProperty("SourceObject").Name,
                typeof(EventResponseSave),
                availableNamedObjects);

            AvailableEvents availableEvents = new AvailableEvents();
            availableEvents.Element = CurrentElement;
            availableEvents.NamedObjectSave = CurrentElement.GetNamedObjectRecursively(instance.SourceObject);
            IncludeMember(typeof(EventResponseSave).GetProperty("SourceObjectEvent").Name,
                typeof(EventResponseSave),
                availableEvents);

        }

        bool DoesCustomVariableCreateEvent(CustomVariable customVariable)
        {
            return customVariable.CreatesEvent;
        }
    }
}
