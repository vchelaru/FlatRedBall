using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Elements;
using FlatRedBall.IO;
using System.IO;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.GuiDisplay.Facades;
using System.Reflection;

namespace FlatRedBall.Glue.Events
{
    public static class EventResponseSaveExtensionMethods
    {
        public static IElement GetContainer(this EventResponseSave instance)
        {
            if (ObjectFinder.Self.GlueProject != null)
            {
                return ObjectFinder.Self.GetElementContaining(instance);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Gets associated EventSave that comes from BuiltInEvents.csv.  This may be null
        /// if the EventResponseSave is an event responding to a changed variable.
        /// </summary>
        public static EventSave GetEventSave(this EventResponseSave instance)
        {
            EventSave toReturn = null;

            string key = "";
            if (!string.IsNullOrEmpty(instance.SourceObject) && !string.IsNullOrEmpty(instance.SourceObjectEvent))
            {
                key = instance.SourceObjectEvent;

                IElement container = ObjectFinder.Self.GetElementContaining(instance);

                if (container != null)
                {
                    NamedObjectSave nos = container.GetNamedObjectRecursively(instance.SourceObject);

                    string type = null;
                    string args = null;

                    if(nos != null)
                    {
                        FlatRedBall.Glue.Plugins.PluginManager.GetEventSignatureArgs(nos, instance, out type, out args);
                    }
                    if(type != null)
                    {
                        toReturn = new EventSave();
                        toReturn.Arguments = args;
                        toReturn.DelegateType = type;
                    }

                    if (toReturn == null && nos != null && nos.SourceType == SourceType.Entity)
                    {

                        // This may be a tunnel into a tunneled event
                        FlatRedBall.Glue.SaveClasses.IElement element = ObjectFinder.Self.GetIElement(nos.SourceClassType);

                        if (element != null)
                        {
                            foreach (EventResponseSave ers in element.Events)
                            {
                                if (ers.EventName == instance.SourceObjectEvent)
                                {
                                    toReturn = ers.GetEventSave();
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                key = instance.EventName;
            }

            if (toReturn == null && EventManager.AllEvents.ContainsKey(key))
            {
                
                toReturn = EventManager.AllEvents[key];
            }

            return toReturn;
        }

        public static bool GetIsTunneling(this EventResponseSave instance)
        {
            return !string.IsNullOrEmpty(instance.SourceObject) && !string.IsNullOrEmpty(instance.SourceObjectEvent);
        }

        public static bool GetIsExposing(this EventResponseSave instance)
        {
            return (instance.GetEventSave() != null && instance.GetEventSave().CreatesEventMember);
        }

        public static string EventResponseSaveToString(this EventResponseSave eventResponseSave)
        {
            return eventResponseSave.EventName + "(Event Response in " + eventResponseSave.GetContainer() + ")";

        }


        public static bool GetIsNewEvent(this EventResponseSave eventResponseSave)
        {
            return !eventResponseSave.GetIsExposing() && !eventResponseSave.GetIsTunneling() &&
                !string.IsNullOrEmpty(eventResponseSave.DelegateType);
        }

        public static bool GetCreatesEvent(this EventResponseSave eventResponseSave)
        {
            return eventResponseSave.GetEventSave() != null &&
                eventResponseSave.GetEventSave().CreatesEventMember;
        }


        public static ParsedMethod GetParsedMethodFromAssociatedFile(this EventResponseSave instance, IElement container, string baseProjectDirectory)
        {
            string fullFileName = EventResponseSave.GetSharedCodeFullFileName(container, baseProjectDirectory);

            return GetParsedMethodFromAssociatedFile(fullFileName, instance);
        }

        public static ParsedMethod GetParsedMethodFromAssociatedFile(string fullFileName, EventResponseSave instance)
        {


            if (File.Exists(fullFileName))
            {
                ParsedFile file = new ParsedFile(fullFileName, false, false);

                if (file.Namespaces.Count != 0)
                {
                    ParsedNamespace parsedNamespace = file.Namespaces[0];

                    if (parsedNamespace.Classes.Count != 0)
                    {
                        ParsedClass parsedClass = parsedNamespace.Classes[0];

                        return parsedClass.GetMethod("On" + instance.EventName);
                    }
                }
            }

            return null;
        }

        public static EventResponseSave GetEventThisTunnelsTo(this EventResponseSave instance, IElement element, out GlueElement containingElement)
        {
            if (instance.GetIsTunneling() == false)
            {
                containingElement = null;
                return null;
            }
            else
            {
                NamedObjectSave nos = element.GetNamedObject(instance.SourceObject);

                if (nos != null && nos.SourceType == SourceType.Entity)
                {
                    containingElement = ObjectFinder.Self.GetElement(nos.SourceClassType);

                    if (containingElement != null)
                    {
                        return containingElement.GetEvent(instance.SourceObjectEvent);
                    }
                }
            }
            containingElement = null;
            return null;
        }

        public static string GetArgsForMethod(this EventResponseSave ers, GlueElement containingElement)
        {
            EventSave eventSave = ers.GetEventSave();


            string args = null;
            bool foundArgs = false;

            if (eventSave != null)
            {
                args = eventSave.Arguments;
                foundArgs = true;
            }
            else if (ers.GetIsTunneling())
            {
                GlueElement tunneledContainingElement;
                EventResponseSave tunneled = ers.GetEventThisTunnelsTo(containingElement, out tunneledContainingElement);

                if (tunneled != null)
                {
                    return tunneled.GetArgsForMethod(tunneledContainingElement);
                }
            }
            else if (!string.IsNullOrEmpty(ers.DelegateType))
            {
                if (ers.DelegateType.StartsWith("System.Action<"))
                {
                    string delegateType = ers.DelegateType;

                    int startOfGeneric = delegateType.IndexOf("<") + 1;
                    int endofGeneric = delegateType.LastIndexOf(">");
                    int lengthOfGeneric = endofGeneric - startOfGeneric;

                    string genericType = delegateType.Substring(startOfGeneric, lengthOfGeneric);

                    args = genericType + " value";
                    foundArgs = true;
                }
                else
                {
                    Type type = TypeManager.GetTypeFromString(ers.DelegateType);

                    if (type != null)
                    {
                        MethodInfo methodInfo = type.GetMethod("Invoke");
                        ParameterInfo[] parameters = methodInfo.GetParameters();
                        for (int i = 0; i < parameters.Length; i++)
                        {
                            if (i != 0)
                            {
                                args += ", ";
                            }

                            ParameterInfo param = parameters[i];

                            args += param.ParameterType.FullName + " " + param.Name;
                            Console.WriteLine("{0} {1}", param.ParameterType.Name, param.Name);
                        }

                        foundArgs = true;
                    }
                }
            }

            if (!foundArgs)
            {
                args = "object sender, EventArgs e";
            }

            return args;

        }

        public static string GetEffectiveDelegateType(this EventResponseSave ers, IElement containingElement)
        {
            if (ers.GetIsTunneling())
            {
                GlueElement tunneledElement;
                EventResponseSave tunneledTo = ers.GetEventThisTunnelsTo(containingElement, out tunneledElement);

                if (tunneledTo != null)
                {
                    return tunneledTo.GetEffectiveDelegateType(tunneledElement);
                }
            }

            EventSave eventSave = ers.GetEventSave();

            string delegateType;
            
            if (eventSave != null && !string.IsNullOrEmpty(eventSave.DelegateType))
            {
                delegateType = eventSave.DelegateType;
            }
            else if (!string.IsNullOrEmpty(ers.DelegateType))
            {
                delegateType = ers.DelegateType;
            }
            else
            {
                delegateType = "EventHandler";
            }

            if(delegateType == "Action")
            {
                delegateType = "System.Action";
            }

            return delegateType;
        }
    }
}
