using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Managers;
using FlatRedBall.IO;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumPlugin.CodeGeneration
{
    public class EventCodeGenerator : Singleton<EventCodeGenerator>
    {


        public void GenerateEvents(ElementSave elementSave, ICodeBlock currentBlock)
        {
            foreach (var eventSave in elementSave.Events.Where(item => !IsHandledByIWindow(item) &&
                (string.IsNullOrEmpty(item.GetSourceObject()) || !string.IsNullOrEmpty(item.ExposedAsName))))
            {

                string name = eventSave.GetRootName();

                if (!string.IsNullOrEmpty(eventSave.ExposedAsName))
                {
                    name = eventSave.ExposedAsName;
                }

                currentBlock.Line("public event FlatRedBall.Gui.WindowEvent " + name + ";");
            }

            List<EventSave> exposedChildrenEvents = GetExposedChildrenEvents(elementSave);

            foreach (var exposedChildEvent in exposedChildrenEvents)
            {
                currentBlock.Line("public event FlatRedBall.Gui.WindowEvent " + exposedChildEvent.ExposedAsName + ";");
            }


            var defaultState = elementSave.DefaultState;
            if (defaultState != null)
            {
                var variablesWithEvents =
                    elementSave.DefaultState.Variables.Where(item =>
                        !string.IsNullOrEmpty(item.ExposedAsName) &&
                        GetIfShouldGenerateEventOnVariableSet(elementSave, item));

                foreach (var variable in variablesWithEvents)
                {
                    currentBlock.Line($"public event System.EventHandler {GetEventName(variable, elementSave)};");
                }
            }
        }

        public List<EventSave> GetExposedChildrenEvents(ElementSave elementSave)
        {
            var exposeEventsAsObject = elementSave.GetValueFromThisOrBase("ExposeChildrenEvents");

            bool exposeChildrenEvents = false;
            if (exposeEventsAsObject is bool)
            {
                exposeChildrenEvents = (bool)elementSave.GetValueFromThisOrBase("ExposeChildrenEvents");
            }

            List<EventSave> exposedChildrenEvents = new List<EventSave>();


            if (exposeChildrenEvents)
            {
                var defaultState = elementSave.DefaultState;
                if (defaultState != null)
                {
                    foreach (var child in elementSave.Instances)
                    {
                        var hasEventsAsObject =
                            defaultState.GetValueRecursive(child.Name + ".HasEvents");
                        
                        if(hasEventsAsObject is bool hasEvents)
                        {

                            if (hasEvents)
                            {
                                EventSave eventSave = new EventSave();
                                eventSave.Name = $"{child.MemberNameInCode()}.Click";
                                eventSave.ExposedAsName = $"{child.MemberNameInCode()}Click";

                                exposedChildrenEvents.Add(eventSave);
                            }

                        }
                    }
                }

            }

            return exposedChildrenEvents;
        }

        private bool IsHandledByIWindow(EventSave item)
        {
            // Specifically check "Name" because we want to create events for events on instances
            if (item.Name == "Click" ||
                item.Name == "RollOn" ||
                item.Name == "RollOff" ||
                item.Name == "RollOver"
                )
            {
                return true;
            }

            return false;
        }

        public string GetEventName(VariableSave variable, ElementSave container)
        {
            if (!string.IsNullOrEmpty(variable.ExposedAsName))
            {
                return $"{variable.ExposedAsName.Replace(" ", "")}Changed";
            }
            else
            {
                return $"{variable.MemberNameInCode(container)}Changed";
            }
        }

        internal void HandleGetEventSignatureArgs(FlatRedBall.Glue.SaveClasses.NamedObjectSave namedObject, FlatRedBall.Glue.Events.EventResponseSave eventResponseSave,
            out string type, out string args)
        {
            bool isGumFile = false;
            type = null;
            args = null;

            if(namedObject != null)
            {
                bool isFromFile = namedObject.SourceType == FlatRedBall.Glue.SaveClasses.SourceType.File;
                string extension = null;
                if(isFromFile && !string.IsNullOrEmpty(namedObject.SourceFile))
                {
                    extension = FileManager.GetExtension(namedObject.SourceFile);
                }


                isGumFile = extension == "gusx" || extension == "gucx";
            }
            if(isGumFile)
            {
                type = "FlatRedBall.Gui.WindowEvent";
                args = "FlatRedBall.Gui.IWindow window";
            }
        }

        public bool GetIfShouldGenerateEventOnVariableSet(ElementSave elementSave, VariableSave variable)
        {
            return true;
        }

    }
}
